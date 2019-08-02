<template>
  <div class="transComp">
    <section class="transComp_databaseSec">
      <button-comp
          :css_variables="button_css"
          v-on:button_comp_clicked="select_dbfolder">Database Folder...
      </button-comp>
      <label-comp
          heading="Database Folder"
          header_position="below"
          :value="db_folder"
          :css_variables="label_css">
      </label-comp>
      <select-edit-comp
          heading="Database"
          placeholder="Select/Add a database"
          input_size="20"
          :items="db_file_names"
          :single_border="single_border"
          :css_variables="select_edit_css"
          v-on:select_edit_comp_value_changed="db_changed">
      </select-edit-comp>
    </section>
    <section class="transComp_tableSec">
      <section class="transComp_filterTransSec">
        <section class="transComp_filterSelectSec">
          <select-comp
              heading="Type"
              placeholder="Select a type"
              drop_panel_width="120px"
              :items="filter_types"
              :single_border="single_border"
              :css_variables="select_css"
              :select_value="filter_type"
              v-on:select_comp_value_changed="value => {this.filter_type = value}">
          </select-comp>
          <select-comp
              heading="Category"
              placeholder="Select a category"
              :items="filter_categories"
              :single_border="single_border"
              :css_variables="select_css"
              :select_value="filter_category"
              v-on:select_comp_value_changed="value => {this.filter_category = value}">
          </select-comp>
          <select-comp
              heading="Year"
              placeholder="Select a year"
              drop_panel_width="120px"
              :items="filter_years"
              :single_border="single_border"
              :css_variables="select_css"
              :select_value="filter_year"
              v-on:select_comp_value_changed="value => {this.filter_year = value}">
          </select-comp>
          <button-comp
              v-on:button_comp_clicked="filter_transactions"
              :css_variables="button_css">Filter Table
          </button-comp>
        </section>
        <section class="transComp_statsSec">
          <div><strong>Total($): </strong><span>{{total}}</span></div>
          <div><strong>Count: </strong><span>{{count}}</span></div>
        </section>
      </section>
      <table-comp
          title="Transactions"
          :rows="table_rows"
          :headings="table_headings"
          :column_widths="table_col_widths"
          :css_variables="table_css"
          v-on:table_comp_row="table_row_selected">
      </table-comp>
    </section>
    <section class="transComp_inputSec">
      <select-comp
          heading="Type"
          placeholder="Select a type"
          input_size="8"
          drop_panel_height="50px"
          :items="input_types"
          :single_border="single_border"
          :css_variables="select_css"
          :select_value="input_type"
          v-on:select_comp_value_changed="value => {this.input_type = value}">
      </select-comp>
      <select-edit-comp
          heading="Category"
          placeholder="Select/Add a category"
          input_size="16"
          :items="input_categories"
          :single_border="single_border"
          :css_variables="select_edit_css"
          :select_value="input_category"
          v-on:select_edit_comp_value_changed="value => {this.input_category = value}">
      </select-edit-comp>
      <input-comp
          heading="Date"
          header_position="top"
          input_type="date"
          :single_border="single_border"
          :css_variables="input_css"
          :input_value="input_date"
          v-on:input_comp_value_changed="value => {this.input_date = value}">
      </input-comp>
      <input-comp
          heading="Amount"
          placeholder="Enter amt"
          header_position="top"
          input_size="6"
          :single_border="single_border"
          :css_variables="input_css"
          :input_value="input_amount"
          v-on:input_comp_value_changed="value => {this.input_amount = value}">
      </input-comp>
      <input-comp
          heading="Source"
          placeholder="Enter source"
          header_position="top"
          input_size="20"
          :single_border="single_border"
          :css_variables="input_css"
          :input_value="input_source"
          v-on:input_comp_value_changed="value => {this.input_source = value}">
      </input-comp>
      <input-comp
          class="transComp_inputSec_description"
          heading="Description"
          placeholder="Enter description"
          header_position="top"
          input_size="30"
          :single_border="single_border"
          :css_variables="input_css"
          :input_value="input_description"
          v-on:input_comp_value_changed="value => {this.input_description = value}">
      </input-comp>
    </section>
    <section class="transComp_buttonSec">
      <button-comp
          v-on:button_comp_clicked="add_transaction"
          :css_variables="button_css">Add
      </button-comp>
      <button-comp
          v-on:button_comp_clicked="update_transaction"
          :css_variables="button_css">Update
      </button-comp>
      <button-comp
          v-on:button_comp_clicked="delete_transaction"
          :css_variables="button_css">Delete
      </button-comp>
      <button-comp
          v-on:button_comp_clicked="cancel_transaction"
          :css_variables="button_css">Cancel
      </button-comp>
      <button-comp
          v-on:button_comp_clicked="create_excel_sheet"
          :css_variables="button_css">Excel Sheet
      </button-comp>
    </section>
    <section class="transComp_statusSec">{{status_content}}</section>
  </div>    
</template>

<script>
  import Vue from 'vue';
  import ButtonComp from 'buttoncomp';
  import InputComp from 'inputcomp';
  import LabelComp from 'labelcomp'
  import SelectComp from 'selectcomp';
  import SelectEditComp from 'selecteditcomp'
  import TableComp from 'tablecomp';
  
  export default {
    name: "App",
    data: function() {
      return {
        localhost: 'http://localhost:8080/moneytracker',
        db_folder: null,
        db_file_names: null,
        db_files: null,

        filter_types: ['all','expense','income'],
        filter_categories: ['all'],
        filter_years: ['all'],

        filter_type: null,
        filter_category: null,
        filter_year: null,

        action: null,
        current_id: null,
        backup_transaction: null,

        input_types: ['expense','income'],
        input_categories: null,
        input_years: null,

        input_type: null,
        input_category: null,
        input_date: null,
        input_amount: null,
        input_source: null,
        input_description: null,

        total: 0,
        count: 0,
        status_content: "Status",

        trans_bus: new Vue(),

        button_css: {
          button_comp_font_size: ".6rem",
          button_comp_background: "linear-gradient(to bottom, #a3ff33 0, #3cc72a 13%, #2eba1e 33%, #4bba37 64%, #4bba37 100%)",
          button_comp_hover_background: "linear-gradient(to bottom, #a3ff33 0, #4bba37 100%)"
        },

        label_css: {
          label_comp_heading_color: "white",
          label_comp_value_color: "white",
          label_comp_scrollbar_color: "lightgreen"
        },

        select_edit_css: {
          select_edit_heading_color: "White",
          select_edit_arrow_color: "white",
          select_edit_color: "white",
          select_edit_placeholder_color: "white",
          select_edit_border_color: "white",
          select_edit_items_panel_position: "absolute",
          select_edit_items_panel_z_index: "100",
          select_edit_items_panel_color: "white",
          select_edit_items_panel_background: "#42b068",
          select_edit_items_panel_border: "1px solid white",
          select_edit_item_hover_color: "gold",
        },

        select_css: {
          select_comp_heading_color: "white",
          select_comp_color: "white",
          select_comp_border_color: "white",
          select_comp_items_panel_position: "absolute",
          select_comp_items_panel_z_index: "100",
          select_comp_items_panel_color: "white",
          select_comp_items_panel_background: "#42b068",
          select_comp_item_hover_color: "gold",
        },

        table_rows: null,
        table_headings: ['id','type', 'category', 'date', 'amount($)', 'source', 'description'],
        table_col_widths: [70,70, 100, 100, 100, 160, 350],
        table_css: {
          table_comp_title_color: "white",
          table_comp_thead_color: "white",
          table_comp_thead_background: "transparent",
          table_comp_thead_border_bottom: "2px solid white",
          table_comp_row_color: "white",
          table_comp_row_border_bottom: "1px solid white",
          table_comp_row_selected_color: "gold",
          table_comp_row_odd_background: "green",
          table_comp_row_even_background: "green",
          table_comp_cell_font_size: "1rem"
        },

        input_css: {
          input_comp_input_font_size: "1rem",
          input_comp_heading_color: "white",
          input_comp_input_color: "white",
          input_comp_input_border_color: "white",
          input_comp_input_placeholder_color: "white",
          input_comp_input_focus_background: "green"
        },

        single_border: true
      }
    },
    components: {
      ButtonComp,
      InputComp,
      LabelComp,
      SelectEditComp,
      SelectComp,
      TableComp
    },
    mounted() {
      //set up 'status_changed' event
      this.trans_bus.$on('status_changed',(message) => {
        this.status_content = message;
      });
    },
    methods: {
      filter_transactions: function() {
        //get filtered transactions from server
        const url=this.localhost;
        const request_data={
          action: 'filterTransactions',
          selection: {
            type: this.filter_type,
            category: this.filter_category,
            year: this.filter_year.toString()
          }
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          const trans_array=JSON.parse(resp_str);
          this.table_rows = [];
          let total = 0;
          for(let trans of trans_array){
            const row = [
              [trans.Id,''],
              [trans.TransType,''],
              [trans.CategoryName,''],
              [trans.TransDate,''],
              [(+trans.Amount).toFixed(2),''],
              [trans.Source,''],
              [trans.Description,'']
            ];
            total += trans.Amount;
            this.table_rows.push(row);
          }
          this.count = this.table_rows.length;
          this.total = (+total).toFixed(2);
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Filter transactions error: ${error.message}`)
        });
      },
      //select database folder
      select_dbfolder: function() {
        const url=this.localhost;
        const request_data={
          action: 'getDbFiles'
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok) {
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          const resp_dict=JSON.parse(resp_str);
          this.db_folder = resp_dict.dbfolder;

          //set SelectEditComp items (database file names) in database folder
          this.db_files = {};
          const file_paths = resp_dict.dbfilepaths;
          this.db_file_names = resp_dict.dbfilenames;
          for(let i=0; i<this.db_file_names.length; i++){
            this.db_files[this.db_file_names[i]] = file_paths[i];
          }
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Get database files error: ${error.message}`)
        });
      },
      //SelectEditComp child to parent event callback for database name selection
      db_changed: function(value) {
        const db_name = value;
        if(db_name !== null) {
          let db_path = null;
          if(this.db_files[db_name] !== undefined){
            db_path = this.db_files[db_name];
          }else {
            if(db_name.indexOf('.db') !== -1){
              db_path = `${this.db_folder}\\${db_name}`;
            }else {
              db_path = `${this.db_folder}\\${db_name}.db`;
            }
          }

          //select database name
          const url=this.localhost;
          const request_data={
            action: 'selectdb',
            dbpath: db_path
          };
          const request_data_str=JSON.stringify(request_data);
          const config={
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          fetch(url, config).then(response => {
            if(response.ok) {
              return response.text();
            }
            throw new Error(response.statusText);
          }).then(resp_str => {
            const resp_dict=JSON.parse(resp_str);

            this.input_categories = resp_dict.categorynames;

            this.filter_type = 'all';

            this.filter_categories = resp_dict.categorynames.slice(0);
            this.filter_categories.splice(0,0,'all');
            this.filter_category = 'all';

            this.filter_years = resp_dict.years;
            this.filter_years.splice(0,0,'all');
            this.filter_year = 'all';

            this.filter_transactions();

            this.trans_bus.$emit('status_changed', `Server selected ${db_path}`);
          }).catch(error => {
            this.trans_bus.$emit('status_changed', `Select database name error: ${error.message}`)
          });
        }else {
          this.trans_bus.$emit('status_changed', 'Database name has not been entered/selected')
        }
      },
      table_row_selected: function(obj) {
        this.current_id = obj.row_values[0];
        this.input_type = obj.row_values[1];
        this.input_category = obj.row_values[2];
        this.input_date = obj.row_values[3];
        this.input_amount = obj.row_values[4];
        this.input_source = obj.row_values[5];
        this.input_description = obj.row_values[6];
      },
      //ButtonComp child to parent event callback to add a transaction to database
      add_transaction: function(transaction,isCancel) {
        this.action = 'Add';
        if(!isCancel) { //transaction is coming from user inputs
          transaction = {};
          if(this.input_amount.toString().indexOf('$') === -1) {
            transaction.TransType=this.input_type;
            transaction.CategoryName=this.input_category;
            transaction.TransDate=this.input_date;
            transaction.Amount=this.input_amount;
            transaction.Source=this.input_source;
            transaction.Description=this.input_description;
          }else {
            this.trans_bus.$emit('status_changed','Warning: Please do not include the "$" sign in the amount.');
            return
          }
        }

        const url=this.localhost;
        const request_data={
          action: 'addTransaction',
          transaction: transaction
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          const resp_dict=JSON.parse(resp_str);
          const trans_dict = resp_dict.transaction;

          this.backup_transaction = trans_dict;
          this.backup_transaction.Amount = this.backup_transaction.Amount.toString();

          this.current_id = trans_dict.Id;
          this.input_categories = resp_dict.categorynames;

          this.filter_categories = resp_dict.categorynames.slice(0);
          this.filter_categories.splice(0,0,'all');

          this.filter_years = resp_dict.years;
          this.filter_years.splice(0,0,'all');

          //filter transaction table TableComp
          this.filter_transactions();
          if(isCancel){
            this.trans_bus.$emit('status_changed', `Successfully cancelled delete transaction with id ${this.current_id}`);
          }else {
            this.trans_bus.$emit('status_changed', `Successfully added transaction with id ${this.current_id}`);
          }
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Add transaction error: ${error.message}`)
        });
      },
      //ButtonComp child to parent event callback to update a transaction to database
      update_transaction: function(transaction,isCancel) {
        this.action = 'Update';
        if(!isCancel) { //transaction is coming from user inputs
          transaction = {};
          transaction.Id = this.current_id;
          if(this.input_amount.toString().indexOf('$') === -1) {
            transaction.TransType=this.input_type;
            transaction.CategoryName=this.input_category;
            transaction.TransDate=this.input_date;
            transaction.Amount=this.input_amount;
            transaction.Source=this.input_source;
            transaction.Description=this.input_description;
          }else {
            this.trans_bus.$emit('status_changed','Warning: Please do not include the "$" sign in the amount.');
            return
          }
        }
        const url=this.localhost;
        const request_data={
          action: 'updateTransaction',
          transaction: transaction
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          const resp_dict=JSON.parse(resp_str);

          this.backup_transaction = resp_dict.backup;
          this.backup_transaction.Amount = this.backup_transaction.Amount.toString();

          this.input_categories = resp_dict.categorynames;

          this.filter_categories = resp_dict.categorynames.slice(0);
          this.filter_categories.splice(0,0,'all');

          this.filter_years = resp_dict.years;
          this.filter_years.splice(0,0,'all');
          //filter transaction table TableComp
          this.filter_transactions();
          if(isCancel){
            this.trans_bus.$emit('status_changed', `Successfully cancelled update transaction with id ${this.current_id}`);
          }else {
            this.trans_bus.$emit('status_changed', `Successfully updated transaction with id ${this.current_id}`);
          }
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Update transaction error: ${error.message}`)
        });
      },
      //ButtonComp child to parent event callback to delete a transaction from database
      delete_transaction: function(transaction,isCancel) {
        this.action = 'Delete';
        if(!isCancel) { //transaction is coming from user inputs
          transaction = {};
          transaction.Id = this.current_id;
          transaction.TransType=this.input_type;
          transaction.CategoryName=this.input_category;
          transaction.TransDate=this.input_date;
          transaction.Amount=this.input_amount;
          transaction.Source=this.input_source;
          transaction.Description=this.input_description;
        }
        const url=this.localhost;
        const request_data={
          action: 'deleteTransaction',
          transaction: transaction
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          const resp_dict=JSON.parse(resp_str);

          this.backup_transaction = resp_dict.backup;
          this.backup_transaction.Amount = this.backup_transaction.Amount.toString();

          this.input_categories = resp_dict.categorynames;

          this.filter_categories = resp_dict.categorynames.slice(0);
          this.filter_categories.splice(0,0,'all');

          this.filter_years = resp_dict.years;
          this.filter_years.splice(0,0,'all');
          //filter transaction table TableComp
          this.filter_transactions();
          if(isCancel){
            this.trans_bus.$emit('status_changed', `Successfully cancelled add transaction with id ${this.current_id}`);
          }else {
            this.trans_bus.$emit('status_changed', `Successfully deleted transaction with id ${this.current_id}`);
          }
          //set the input components to null
          this.trans_bus.$emit('select_comp_select','input_type',null);
          this.trans_bus.$emit('select_edit_select','input_category',null);
          this.trans_bus.$emit('date_comp_set_value','input_date',null);
          this.trans_bus.$emit('input_comp_set_value','input_amt',null);
          this.trans_bus.$emit('input_comp_set_value','input_source',null);
          this.trans_bus.$emit('input_comp_set_value','input_desc',null);
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Update transaction error: ${error.message}`)
        });
      },
      //ButtonComp child to parent event callback to cancel a transaction from database
      cancel_transaction: function() {
        switch(this.action){
          case 'Add':
            this.delete_transaction(this.backup_transaction,true);
            break;
          case 'Update':
            this.update_transaction(this.backup_transaction,true);
            break;
          case 'Delete':
            this.add_transaction(this.backup_transaction,true);
            break;
        }
      },
      create_excel_sheet: function() {
        let year = this.filter_year;
        if(year === 'all'){
          year = 'All Years';
        }

        const table_rows = [];
        this.table_rows.forEach(row => {
          const new_row = [];
          row.forEach(cell => {
            new_row.push(cell[0]);
          });
          table_rows.push(new_row);
        });
        
        const url=this.localhost;
        const request_data={
          action: 'excelSheet',
          year: String(year),
          rows: table_rows
        };
        const request_data_str=JSON.stringify(request_data);
        const config={
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          this.trans_bus.$emit('status_changed',resp_str);
        }).catch(error => {
          this.trans_bus.$emit('status_changed', `Excel sheet creation error: ${error.message}`)
        });
      }
    }
  }
</script>

<style lang="less">
  .transComp {
    display: flex;
    flex-direction: column;
    align-items: center;
    background: seagreen;
    width: 100%;
    height: 100%;
    padding: 1rem;
    font-family: Verdana,serif;

    &_databaseSec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      align-items: center;
      min-width: 50rem;
    }

    &_tableSec {
      margin-top: 2.5rem;
    }

    &_filterTransSec {
      display: flex;
      flex-direction: row;
      align-items: center;
      margin: 1.25rem 0 3.75rem 0;
    }

    &_filterSelectSec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      min-width: 44rem;
      align-items: center;
    }

    &_statsSec {
      font-size: 1.125rem;
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      color: white;
      min-width: 18rem;
      margin-left: 4rem;
    }

    &_inputSec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      align-items: flex-end;
      width: 75rem;
      margin-top: 2rem;
      
      &_description {
        margin-left: 60px;
      }
    }

    &_buttonSec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      min-width: 25rem;
      margin-top: 2.5rem;
    }

    &_statusSec {
      margin-top: 2.5rem;
      font-size: 1.125rem;
      width: 100%;
      color: white;
    }
  }
</style>