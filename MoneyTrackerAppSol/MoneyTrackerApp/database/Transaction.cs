using System;
using System.Collections.Generic;

namespace MoneyTracker.database {
  public class Transaction {
    public int Id {get;set;}
    public int Year {get;set;}
    public int YearId {get;set;}
    public int CategoryId {get;set;}
    public string CategoryName {get;set;}
    public string TransType {get;set;}
    public DateTime TransDate {get;set;}
    public decimal Amount {get;set;}
    public string Description {get;set;}
    public string Source {get;set;}
    

    public Dictionary<string, object> TransactionDict() {
      string date = String.Format("{0:d}", TransDate);
      return new Dictionary<string, object>() {
        {"Id", Id},
        {"Year", Year},
        {"YearId",YearId},
        {"CategoryId", CategoryId},
        {"CategoryName", CategoryName},
        {"TransType", TransType},
        {"TransDate", date},
        {"Amount", Amount},
        {"Description", Description},
        {"Source", Source}
      };
    }
  }
}