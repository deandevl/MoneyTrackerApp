using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using BasicWebServerLib;
using BasicWebServerLib.Events;
using BasicWebServerLib.HttpCommon;
using LiteDB;
using MoneyTracker.database;
using Excel = Microsoft.Office.Interop.Excel;

namespace MoneyTrackerApp {
  public class Handlers {
    private readonly string _serverBaseFolder;
    private readonly JavaScriptSerializer _serializer;
    private readonly Helpers _helpers;
    private readonly Dictionary<string, Action> _actions;
    private Dictionary<string, object> _requestDictionary;
    private HttpConnectionDetails _httpDetails;
    private LiteCollection<TransYear> _yearCollection;
    private LiteCollection<TransCategory> _categoryCollection;
    private LiteCollection<Transaction> _transactionCollection;
    
    public Handlers(string serverBaseFolder) {
      _serverBaseFolder = serverBaseFolder;
      _serializer = new JavaScriptSerializer();
      _helpers = new Helpers();
      _actions = new Dictionary<string, Action>() {
        {"getDbFiles", () => {
          try {
            ArrayList dbFilePaths = new ArrayList();
            ArrayList dbFileNames = new ArrayList();
            
            
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            Invoker invoker = new Invoker(fbd);
            if(DialogResult.OK == invoker.Invoke()) {
              string[] files = Directory.GetFiles(fbd.SelectedPath);
              foreach(string filepath in files) {
                FileInfo fileInfo = new FileInfo(filepath);
                if(fileInfo.Extension.Equals(".db")) {
                  dbFilePaths.Add(filepath);
                  dbFileNames.Add(fileInfo.Name);
                }
              }
            }
            
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"dbfolder", fbd.SelectedPath},
              {"dbfilepaths", dbFilePaths},
              {"dbfilenames", dbFileNames}
            };
            string responseStr = _serializer.Serialize(responseDict);;
            _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"selectdb", () => {
          try {
            string dbpath = (string)_requestDictionary["dbpath"];
            LiteDatabase db = new LiteDatabase(dbpath);
            _yearCollection = db.GetCollection<TransYear>("YearCollection");
            _categoryCollection = db.GetCollection<TransCategory>("CategoryCollection");
            _transactionCollection = db.GetCollection<Transaction>("TransactionCollection");
            
            //get categories and year for client selection from this database
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"categorynames", GetCategoryNames()},
              {"years", GetYears()}
            };
            string responseStr = _serializer.Serialize(responseDict);
            
            _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"filterTransactions", () => {
          try {
            if(_transactionCollection != null) {
              Dictionary<string, object> selectionDict = (Dictionary<string, object>)_requestDictionary["selection"];
              string type = (string)selectionDict["type"];
              string category = (string)selectionDict["category"];
              string year = (string)selectionDict["year"];

              ArrayList typeList = new ArrayList();
              if(type == "all") {
                typeList.Add("income");
                typeList.Add("expense");
              } else {
                typeList.Add(type);
              }

              ArrayList categoryList = null;
              if(category == "all") {
                categoryList = GetCategoryNames();
              } else {
                categoryList = new ArrayList();
                categoryList.Add(category);
              }
              ArrayList yearList = null;
              if(year == "all") {
                yearList = GetYears();
              } else {
                yearList = new ArrayList();
                yearList.Add(Int32.Parse(year));
              }

             IEnumerable<Transaction> transactions = _transactionCollection.Find(x => typeList.Contains(x.TransType) && categoryList.Contains(x.CategoryName) && yearList.Contains(x.Year));
              
              ArrayList transactionList = new ArrayList();
              foreach(Transaction trans in transactions) {
                transactionList.Add(trans.TransactionDict());
              }
              string responseStr = _serializer.Serialize(transactionList);
              _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
            }
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"addTransaction", () => {
          try {
            if(_transactionCollection != null) {
              Transaction newTransaction = new Transaction();
              Dictionary<string, object> transactionDict = (Dictionary<string, object>)_requestDictionary["transaction"];
              string categoryName = (string)transactionDict["CategoryName"];
              string[] dateParts = ((string)transactionDict["TransDate"]).Split('-');
              newTransaction.TransDate = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]),
                Int32.Parse(dateParts[2]));
              
              int year = Int32.Parse(dateParts[0]);
              int categoryId = GetCategoryId(categoryName);
              int yearId = GetYearId(year);

              newTransaction.Year = year;
              newTransaction.YearId = yearId;
              newTransaction.CategoryName = categoryName;
              newTransaction.CategoryId = categoryId;
              newTransaction.TransType = (string)transactionDict["TransType"];
              
              newTransaction.Amount = decimal.Parse((string)transactionDict["Amount"]);
              newTransaction.Description = (string)transactionDict["Description"];
              newTransaction.Source = (string)transactionDict["Source"];

              newTransaction.Id = _transactionCollection.Insert(newTransaction);
              //return a list of category names, years, and the new transaction
              Dictionary<string, object> responseDict = new Dictionary<string, object>() {
                {"categorynames", GetCategoryNames()},
                {"years", GetYears()},
                {"transaction", newTransaction.TransactionDict()}
              };
              string responseStr = _serializer.Serialize(responseDict);
              _helpers.SendHttpTextResponse(_httpDetails.Response, responseStr);
            } else {
              _helpers.SendHttpResponse(400, "Database has not been specified", new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
            }
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"updateTransaction", () => {
          try {
            if(_transactionCollection != null) {
              Dictionary<string, object> transactionDict = (Dictionary<string, object>)_requestDictionary["transaction"];
              //get the current transaction for backup
              int id = Convert.ToInt32(transactionDict["Id"]);
              Transaction backupTransaction = _transactionCollection.FindById(id);
              //build the new updated transaction
              Transaction updateTransaction = new Transaction();
              string categoryName = (string)transactionDict["CategoryName"];
              string[] dateParts = ((string)transactionDict["TransDate"]).Split('-');
              
              updateTransaction.TransDate = new DateTime(Int32.Parse(dateParts[0]), Int32.Parse(dateParts[1]),
                Int32.Parse(dateParts[2]));
              int year = Int32.Parse(dateParts[0]);
              int categoryId = GetCategoryId(categoryName);
              int yearId = GetYearId(year);

              updateTransaction.Id = id;
              updateTransaction.Year = year;
              updateTransaction.YearId = yearId;
              updateTransaction.CategoryName = categoryName;
              updateTransaction.CategoryId = categoryId;
              updateTransaction.TransType = (string)transactionDict["TransType"];
              //string fullName = transactionDict["Amount"].GetType().FullName;
              updateTransaction.Amount = decimal.Parse((string)transactionDict["Amount"]);
              updateTransaction.Description = (string)transactionDict["Description"];
              updateTransaction.Source = (string)transactionDict["Source"];

              bool found = _transactionCollection.Update(updateTransaction);
              if(found) {
                //return a list of category names, years, and the backup transaction
                Dictionary<string, object> responseDict = new Dictionary<string, object>() {
                  {"categorynames", GetCategoryNames()},
                  {"years", GetYears()},
                  {"backup", backupTransaction.TransactionDict()}
                };
                string responseStr = _serializer.Serialize(responseDict);
                _helpers.SendHttpTextResponse(_httpDetails.Response, responseStr);
              } else {
                _helpers.SendHttpResponse(400, "Transaction was not located for update", new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
              }
            } else {
              _helpers.SendHttpResponse(400, "Database has not been specified", new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
            }
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"deleteTransaction", () => {
          try {
            if(_transactionCollection != null) {
              Dictionary<string, object> transactionDict = (Dictionary<string, object>)_requestDictionary["transaction"];
              //get the current transaction for backup
              int id = Convert.ToInt32(transactionDict["Id"]);
              Transaction backupTransaction = _transactionCollection.FindById(id);
              //get the current transaction's category id and year id
              int categoryId = backupTransaction.CategoryId;
              int yearId = backupTransaction.YearId;
              
              //delete the transaction
              bool found = _transactionCollection.Delete(id);
              if(found) {
                //was this the last transaction with the category?
                if(!_transactionCollection.Exists(Query.EQ("CategoryId",categoryId))){
                  //if category does not exist among transactons then delete it
                  _categoryCollection.Delete(categoryId);

                }
                //was this the last transaction with the year?
                if(!_transactionCollection.Exists(Query.EQ("YearId", yearId))) {
                  _yearCollection.Delete(yearId);
                }
                //return a list of category names, years, and the backup transaction
                Dictionary<string, object> responseDict = new Dictionary<string, object>() {
                  {"categorynames", GetCategoryNames()},
                  {"years", GetYears()},
                  {"backup", backupTransaction.TransactionDict()}
                };
                string responseStr = _serializer.Serialize(responseDict);
                _helpers.SendHttpTextResponse(_httpDetails.Response, responseStr);
              } else {
                _helpers.SendHttpResponse(400, "Transaction was not located for delete", new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
              }
            }
          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message,new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"excelSheet", () => {
          try {
            float totalExpense = 0;
            float totalIncome = 0;
            string year = (string)_requestDictionary["year"];
            ArrayList rows = (ArrayList)_requestDictionary["rows"];
            Dictionary<int, object> expenseDictionary = new Dictionary<int, object>();
            Dictionary<string,float> expenseTotals = new Dictionary<string, float>();
            Dictionary<int, object> incomeDictionary = new Dictionary<int, object>();
            Dictionary<string,float> incomeTotals = new Dictionary<string, float>();

            foreach(ArrayList row in rows) {
              string category = (string)row[2];
              string date = (string)row[3];
              string amount = row[4].ToString();
              string source = (string)row[5];
              string description = (string)row[6];
              float amountFloat = float.Parse(amount);
              if(row[1].Equals("expense")) {
                totalExpense += amountFloat;
                expenseDictionary.Add((int)row[0], new List<string>(){amount,category,date,source,description});
                if(!expenseTotals.ContainsKey(category)) {
                  expenseTotals.Add(category,amountFloat);
                } else {
                  expenseTotals[category] = expenseTotals[category] + amountFloat;
                }
              }else if(row[1].Equals("income")) {
                totalIncome += amountFloat;
                incomeDictionary.Add((int)row[0], new List<string>(){amount,category,date,source,description});
                if(!incomeTotals.ContainsKey(category)) {
                  incomeTotals.Add(category,amountFloat);
                } else {
                  incomeTotals[category] = incomeTotals[category] + amountFloat;
                }
              }
            }
 
            //Create Excel App
            Excel.Application excelApp = new Excel.Application();
            excelApp.Visible = true;

            Excel.Workbook workbook = excelApp.Workbooks.Add(Missing.Value);
            /*
            //Create Excel workbook at file location
            string file_location = Path.Combine(_databasePath, "moneytracker.xls");
            excelApp.Workbooks.Open(file_location, 0, false, 5, "", "",
              false, Excel.XlPlatform.xlWindows, "", true, false, 0, true, false, false);
            */  
            
            string amtcol = ColumnNumberToName(2);
            string catcol = ColumnNumberToName(3);
            string datcol = ColumnNumberToName(4);
            string soucol = ColumnNumberToName(5);
            string descol = ColumnNumberToName(6);

            //Create expense worksheet
            if(expenseDictionary.Count() > 0) {
              //expenseSheet = (Excel.Worksheet)sheets.get_Item(1);
              Excel.Worksheet expenseSheet = (Excel.Worksheet)workbook.ActiveSheet;
              expenseSheet.Name = "Expenses";
              
              //create expense title cell
              expenseSheet.Cells[2, 2] = "Expenses for " + year;
              expenseSheet.get_Range(amtcol+2, catcol+2).Font.Bold = true;
              expenseSheet.get_Range(amtcol+2, catcol+2).Font.Size = 22;
              
              //set column widths for headings, value cells
              expenseSheet.get_Range(amtcol + 3, amtcol + 200).ColumnWidth = 8;
              expenseSheet.get_Range(catcol + 3, catcol + 200).ColumnWidth = 20;
              expenseSheet.get_Range(datcol + 3, datcol + 200).ColumnWidth = 12;
              expenseSheet.get_Range(soucol + 3, soucol + 200).ColumnWidth = 20;
              expenseSheet.get_Range(descol + 3, descol + 200).ColumnWidth = 45;
              
              //some twiks
              expenseSheet.get_Range(amtcol + 3, descol + 200).VerticalAlignment = Excel.XlVAlign.xlVAlignTop;
              expenseSheet.get_Range(amtcol + 3, datcol + 200).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
              expenseSheet.get_Range(descol + 3, descol + 200).WrapText = true;
              
              //create transaction heading cells
              expenseSheet.get_Range(amtcol + 4, descol + 4).Font.Bold = true;
              expenseSheet.get_Range(amtcol + 4, descol + 4).Font.Size = 12;
              expenseSheet.Cells[4, 2] = "Amount";
              expenseSheet.Cells[4, 3] = "Category";
              expenseSheet.Cells[4, 4] = "Date";
              expenseSheet.Cells[4, 5] = "Source";
              expenseSheet.Cells[4, 6] = "Description";
              
              //assign values
              for(int idx = 0; idx < expenseDictionary.Count; idx++) {
                var item = expenseDictionary.ElementAt(idx);
                var val =  (List<string>)item.Value;
                int rowI = idx + 5;
                string rowS1 = amtcol + rowI;
                expenseSheet.get_Range(rowS1, rowS1).Formula = "=Fixed(" + val[0] + ",2,TRUE)";
                expenseSheet.Cells[rowI, 3] = val[1];
                expenseSheet.Cells[rowI, 4] = val[2];
                expenseSheet.Cells[rowI, 5] = val[3];
                expenseSheet.Cells[rowI, 6] = val[4];
              }
              
              //create cell for total expense
              int offset = expenseDictionary.Count + 6;
              expenseSheet.get_Range(amtcol + offset, amtcol + offset).Font.Bold = true;
              expenseSheet.get_Range(amtcol + offset, amtcol + offset).Font.Size = 16;
              expenseSheet.get_Range(amtcol + offset, amtcol + offset).Formula = "=Fixed(" + totalExpense + ",2,TRUE)";
            }

            //Create income worksheet at end
            if(incomeDictionary.Count() > 0) {
              Excel.Worksheet incomeSheet = (Excel.Worksheet)workbook.Sheets.Add(Type.Missing, workbook.Sheets[workbook.Sheets.Count],
                1, Excel.XlSheetType.xlWorksheet);
              incomeSheet.Name = "Income";
              
              //create income title cell
              incomeSheet.Cells[2, 2] = "Income for " + year;
              incomeSheet.get_Range(amtcol+2, catcol+2).Font.Bold = true;
              incomeSheet.get_Range(amtcol+2, catcol+2).Font.Size = 22;
              
              //set column widths for headings, value cells
              incomeSheet.get_Range(amtcol + 3, amtcol + 200).ColumnWidth = 8;
              incomeSheet.get_Range(catcol + 3, catcol + 200).ColumnWidth = 20;
              incomeSheet.get_Range(datcol + 3, datcol + 200).ColumnWidth = 12;
              incomeSheet.get_Range(soucol + 3, soucol + 200).ColumnWidth = 20;
              incomeSheet.get_Range(descol + 3, descol + 200).ColumnWidth = 45;
              
              //some twiks
              incomeSheet.get_Range(amtcol + 3, descol + 200).VerticalAlignment = Excel.XlVAlign.xlVAlignTop;
              incomeSheet.get_Range(amtcol + 3, datcol + 200).HorizontalAlignment = Excel.XlHAlign.xlHAlignLeft;
              incomeSheet.get_Range(descol + 3, descol + 200).WrapText = true;
              
              //create transaction heading cells
              incomeSheet.get_Range(amtcol + 4, descol + 4).Font.Bold = true;
              incomeSheet.get_Range(amtcol + 4, descol + 4).Font.Size = 12;
              incomeSheet.Cells[4, 2] = "Amount";
              incomeSheet.Cells[4, 3] = "Category";
              incomeSheet.Cells[4, 4] = "Date";
              incomeSheet.Cells[4, 5] = "Source";
              incomeSheet.Cells[4, 6] = "Description";
              
              //assign values
              for(int idx = 0; idx < incomeDictionary.Count; idx++) {
                var item = incomeDictionary.ElementAt(idx);
                var val = (List<string>)item.Value;
                int rowI = idx + 5;
                string rowS1 = amtcol + rowI;
                incomeSheet.get_Range(rowS1, rowS1).Formula = "=Fixed(" + val[0] + ",2,TRUE)";
                incomeSheet.Cells[rowI, 3] = val[1];
                incomeSheet.Cells[rowI, 4] = val[2];
                incomeSheet.Cells[rowI, 5] = val[3];
                incomeSheet.Cells[rowI, 6] = val[4];
              }
              
              //create cell for total income
              int offset = incomeDictionary.Count + 6;
              incomeSheet.get_Range(amtcol + offset, amtcol + offset).Font.Bold = true;
              incomeSheet.get_Range(amtcol + offset, amtcol + offset).Font.Size = 16;
              incomeSheet.get_Range(amtcol + offset, amtcol + offset).Formula = "=Fixed(" + totalIncome + ",2,TRUE)";
            }
            
            //create a summary worksheet at the end
            Excel.Worksheet summarySheet = (Excel.Worksheet)workbook.Sheets.Add(Type.Missing, workbook.Sheets[workbook.Sheets.Count],
              1, Excel.XlSheetType.xlWorksheet);
            summarySheet.Name = "Summary";
            
            //create expense summary
            if(expenseDictionary.Count() > 0) {
              
              //create cells for summary category totals
              string sumcatcol = ColumnNumberToName(4);
              string sumtotcol = ColumnNumberToName(5);
              
              //create summary expense title
              summarySheet.Cells[2,4] = "Expense Summary";
              summarySheet.get_Range(sumcatcol+2, sumtotcol+2).Font.Bold = true;
              summarySheet.get_Range(sumcatcol+2, sumtotcol+2).Font.Size = 22;
              
              //set column widths for headings, value cells
              summarySheet.get_Range(sumcatcol + 3, sumcatcol + 200).ColumnWidth = 24;
              summarySheet.get_Range(sumtotcol + 3, sumtotcol + 200).ColumnWidth = 12;
              
              //create category total heading cells
              summarySheet.get_Range(sumcatcol + 4, sumtotcol + 4).Font.Bold = true;
              summarySheet.get_Range(sumcatcol + 4, sumtotcol + 4).Font.Size = 16;
              summarySheet.Cells[4, 4] = "Category";
              summarySheet.Cells[4, 5] = "Total";
              
              //assign values
              for(int idx = 0; idx < expenseTotals.Count; idx++) {
                var item = expenseTotals.ElementAt(idx);
                int rowI = idx + 5;
                string rowS2 = sumtotcol + rowI;
                summarySheet.Cells[rowI, 4] = item.Key;
                summarySheet.get_Range(rowS2, rowS2).Formula = "=Fixed(" +  item.Value + ",2,TRUE)";
              }
              
              //create cell for total expense
              int offset = expenseTotals.Count + 6;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Font.Bold = true;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Font.Size = 16;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Formula = "=Fixed(" + totalExpense + ",2,TRUE)";
            }
            
            //create income summary
            if(incomeDictionary.Count() > 0) {
              //create cells for summary category totals
              string sumcatcol = ColumnNumberToName(9);
              string sumtotcol = ColumnNumberToName(10);
              
              //create income header
              summarySheet.Cells[2, 9] = "Income Summary";
              summarySheet.get_Range(sumcatcol+2, sumtotcol+2).Font.Bold = true;
              summarySheet.get_Range(sumcatcol+2, sumtotcol+2).Font.Size = 22;
              
              //set column widths for headings, value cells
              summarySheet.get_Range(sumcatcol + 3, sumcatcol + 200).ColumnWidth = 24;
              summarySheet.get_Range(sumtotcol + 3, sumtotcol + 200).ColumnWidth = 12;
              
              //create category total heading cells
              summarySheet.get_Range(sumcatcol + 4, sumtotcol + 4).Font.Bold = true;
              summarySheet.get_Range(sumcatcol + 4, sumtotcol + 4).Font.Size = 16;
              summarySheet.Cells[4, 9] = "Category";
              summarySheet.Cells[4, 10] = "Total";
              
              //assign values
              for(int idx = 0; idx < incomeTotals.Count; idx++) {
                var item = incomeTotals.ElementAt(idx);
                int rowI = idx + 5;
                string rowS2 = sumtotcol + rowI;
                summarySheet.Cells[rowI, 9] = item.Key;
                summarySheet.get_Range(rowS2, rowS2).Formula = "=Fixed(" +  item.Value + ",2,TRUE)";
              }
              
              //create cell for total income
              int offset = incomeTotals.Count + 6;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Font.Bold = true;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Font.Size = 16;
              summarySheet.get_Range(sumtotcol + offset, sumtotcol + offset).Formula = "=Fixed(" + totalIncome + ",2,TRUE)";
            }
            
            //save changes and close workbook
            workbook.Close(true,Type.Missing,Type.Missing);
            //close Excel server
            excelApp.Quit();
            
            _helpers.SendHttpTextResponse(_httpDetails.Response, "Completed Excel Workbook");

          } catch(Exception ex) {
            _helpers.SendHttpResponse(500, ex.Message, new byte[0], "text/html",
              "MoneyTracker Server", _httpDetails.Response);
          }
        }}
      };
    }

    public void StartServer() {
      BasicWebServer basicServer = new BasicWebServer(baseFolderPath: _serverBaseFolder,tcpPort:null);
      basicServer.HttpRequestChanged += HttpRequestChanged;
      basicServer.Start();
    }

    public void HttpRequestChanged(object sender, EventArgs args) {
      HttpRequestEventArgs httpArgs = (HttpRequestEventArgs)args;
      _httpDetails = httpArgs.Details;
      string body = (string)httpArgs.Body;
      _requestDictionary = _serializer.Deserialize<Dictionary<string, object>>(body);

      if(_httpDetails.HttpPath == "moneytracker") {
        _actions[(string)_requestDictionary["action"]]();
      }
    }

    private int GetCategoryId(string categoryName) {
      TransCategory transCategory = _categoryCollection.FindOne(x => x.Name.Equals(categoryName));
      if(transCategory != null) {
        return transCategory.Id;
      } else {
        transCategory = new TransCategory();
        transCategory.Name = categoryName;
        return _categoryCollection.Insert(transCategory);
      }
    }

    private int GetYearId(int year) {
      TransYear transYear = _yearCollection.FindOne(x => x.Year == year);
      if(transYear != null) {
        return transYear.Id;
      } else {
        transYear = new TransYear();
        transYear.Year = year;
        return _yearCollection.Insert(transYear);
      }
    }
    
    private ArrayList GetCategoryNames() {
      IEnumerable<TransCategory> transCategories = _categoryCollection.FindAll();
      ArrayList categoryNameList = new ArrayList();
      foreach(TransCategory transCategory in transCategories) {
        categoryNameList.Add(transCategory.Name);
      }
      return categoryNameList;
    }

    private ArrayList GetYears() {
      IEnumerable<TransYear> transYears = _yearCollection.FindAll();
      ArrayList yearsList = new ArrayList();
      foreach(TransYear transYear in transYears) {
        yearsList.Add(transYear.Year);
      }
      return yearsList;
    }
    
    // Return the column name for this column number.
    private string ColumnNumberToName(int colNum) {
      // See if it's out of bounds.
      if (colNum < 1) return "A";

      // Calculate the letters.
      string result = "";
      while (colNum > 0) {
        // Get the least significant digit.
        colNum -= 1;
        int digit = colNum % 26;

        // Convert the digit into a letter.
        result = (char)('A' + digit) + result;

        colNum = colNum / 26;
      }
      return result;
    }
  }
  public class Invoker {
    public CommonDialog _invokeDialog;
    private Thread _invokeThread;
    private DialogResult _invokeResult;

    public Invoker(CommonDialog dialog) {
      _invokeDialog = dialog;
      _invokeThread = new Thread(new ThreadStart(InvokeMethod));
      _invokeThread.SetApartmentState(ApartmentState.STA);
      _invokeResult = DialogResult.None;
    }

    public DialogResult Invoke() {
      _invokeThread.Start();
      _invokeThread.Join();
      return _invokeResult;
    }

    private void InvokeMethod() {
      _invokeResult = _invokeDialog.ShowDialog();
    }
  }
}