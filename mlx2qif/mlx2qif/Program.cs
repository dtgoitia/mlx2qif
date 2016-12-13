using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace mlx2qif
{
    class Program
    {
        static void Main(string[] args)
        {
            //string sourceFilePath = "../../../../simpleTest.xml";
            string sourceFilePath = "../../../../aa7books.mlx";
            string accountDicPath = "../../../../accountDictionary.xml";
            string outputFilePath = "../../../../output.qif";
            object[] data = new object[4];
            List<AccountDictionaryEntry> accountDictionary;
            
            // TODO --------------------------------------------------------------- Select MLX file (user input blablaba)
            // TODO --------------------------------------------------------------- Select account library (user input blablaba)
            
            // Parse account dictionary
            accountDictionary = ParseAccountDictionary(accountDicPath);

            // Parse selected MLX file
            data = ParseMlxFile(sourceFilePath);

            // Format Transaction object attribute values (account name, +/- amount, etc.)
            data = FormatData(data, accountDictionary);
            
            // Export to QIF
            List<Transaction> transactionList = data[3] as List<Transaction>;
            ExportToQif(transactionList, outputFilePath);

            Console.Read();
        }

        // -----------------------------------------------------------------------------------------------------
        //
        //      Dictionary methods
        //
        // -----------------------------------------------------------------------------------------------------
        public static List<AccountDictionaryEntry> ParseAccountDictionary ( string filePath )
        // Import and parse XML dictionary, and return it as a list of AccountDictionaryEntry objects
        {
            List<AccountDictionaryEntry> accountDictionary = new List<AccountDictionaryEntry>();
            XmlDocument xmlDoc = new XmlDocument();         // Create an empty instance of XmlDocument object
            xmlDoc.Load(filePath);                          // Populate XmlDocument
            XmlElement xmlEle = xmlDoc.DocumentElement;     // Save the root node at xmlEle

            Console.Write("Importing dictionary...");

            // Run through all nodes at xmlEle
            foreach (XmlNode entryNod in xmlEle)
            {
                accountDictionary = AddDictionaryEntry(accountDictionary, entryNod);
            }

            Console.WriteLine(" done.");
            return accountDictionary;
        }
        public static List<AccountDictionaryEntry> AddDictionaryEntry (List<AccountDictionaryEntry> accountDictionary, XmlNode nod)
        // Add a new element to the provided list and return it
        {
            AccountDictionaryEntry newEntry = new AccountDictionaryEntry();
            newEntry.gnucashAccountName = nod.Attributes["gnucashAccountName"].Value;
            newEntry.moneyloverBook = nod.Attributes["moneyloverBook"].Value;
            newEntry.moneyloverCategory = nod.Attributes["moneyloverCategory"].Value;

            // add newEntry it to the accountDictionary list
            accountDictionary.Add(newEntry);

            // Clear "newEntry" variable
            newEntry = null;

            return accountDictionary;
        }
        public static string GetMoneyLoverAccountName(string accountId, List<Account> accountList)
        // Return Money Lover Account Name (a.k.a. "category" within Money Lover)
        {
            // Find in the list "accountList" the element "x" whose accountId attribute (x.accountId)
            // is equal to the supplied accountId argument, and get its accountName attribute.
            string moneyLoverAccountName = accountList.Find(x => x.accountId == accountId).accountName;
            return moneyLoverAccountName;
        }
        public static string GetMoneyLoverBookName(string accountBookID, List<Book> bookList)
        // Return Money Lover Book Name (a.k.a. "account" within Money Lover)
        {
            // Find in the list "accountList" the element "x" whose accountId attribute (x.accountId)
            // is equal to the supplied accountId argument, and get its accountName attribute.
            Book bookMatch = bookList.Find(x => x.bookId == accountBookID);
            if (bookMatch != null)
            {
                string moneyLoverBookName = bookMatch.bookName;
                return moneyLoverBookName;
            }
            else
            {
                //                Console.WriteLine("\"" + accountBookID + "\" bookId not found between the imported books.");
                return "unknown";
            }

            //string moneyLoverBookName = bookList.Find(x => x.bookId == accountBookID).bookName;
            //return moneyLoverBookName;
            //return "DaKa";
        }
        public static string GetGnuCashAccount(List<AccountDictionaryEntry> accountDictionary, string moneyloverBook, string moneyloverCategory)
        // Return a string with GnuCash account name which matches the given "moneyloverBook" and "moneyloverCategory"
        // GNUCash replace for GnuCash
        {
            List<AccountDictionaryEntry> accountDictionaryMatch = accountDictionary.FindAll(x => x.moneyloverBook.Contains(moneyloverBook));
            if (accountDictionaryMatch != null)
            {
                AccountDictionaryEntry accountDictionaryEntryMatch = accountDictionaryMatch.Find(x => x.moneyloverCategory.Contains(moneyloverCategory));
                if (accountDictionaryEntryMatch != null)
                {
                    string gnucashAccountName = accountDictionaryEntryMatch.gnucashAccountName;
                    return gnucashAccountName;
                }
                else
                {
                    Console.WriteLine("Following book and category not found in supplied dictionary: \"" + moneyloverBook + "\" \"" + moneyloverCategory + "\"");
                    return "unknown";
                }

            }
            else
            {
                Console.WriteLine("\"" + moneyloverBook + " book not found in the supplied dictionary.");
                return "unknown";
            }
            //string gnucashAccountName =
            //    accountDictionary
            //    .FindAll(x => x.moneyloverBook.Contains(moneyloverBook))
            //    .Find(x => x.moneyloverCategory.Contains(moneyloverCategory))
            //    .gnucashAccountName;
            //return gnucashAccountName;


        }
        public static string GetMoneyLoverAccountType(string accountId, List<Account> accountList)
        // Return Money Lover Account Type (according to Money Lover format: 1 income, 2 expense)
        {
            // Find in the list "accountList" the element "x" whose accountId attribute (x.accountId)
            // is equal to the supplied accountId argument, and get its accountType attribute.
            string moneyLoverAccountType = accountList.Find(x => x.accountId == accountId).accountType;
            return moneyLoverAccountType;
        }


        // -----------------------------------------------------------------------------------------------------
        //
        //      MLX methods
        //
        // -----------------------------------------------------------------------------------------------------
        public static object[] ParseMlxFile(string filePath)
        // Import and parse MLX file,
        // and return a 2 element array
        // with category and transaction lists.
        {
            object[] data = new object[4];
            List<Currency> currencyList = new List<Currency>();
            List<Book> bookList = new List<Book>();
            List<Account> accountList = new List<Account>();
            List<Transaction> transactionList = new List<Transaction>();
            XmlDocument xmlDoc = new XmlDocument();         // Create an empty instance of XmlDocument object
            Console.WriteLine("Importing " + filePath + " file.");
            xmlDoc.Load(filePath);                          // Populate XmlDocument
            XmlElement xmlEle = xmlDoc.DocumentElement;     // Save the root node at xmlEle

            // Run through all nodes at xmlEle
            foreach (XmlNode tableNod in xmlEle)
            {
                // Currency
                if (tableNod.Name == "table" && tableNod.Attributes["name"].Value == "currencies")
                {
                    currencyList = ParseCurrencies(tableNod, currencyList);
                }
                // Book
                if (tableNod.Name == "table" && tableNod.Attributes["name"].Value == "accounts")
                {
                    bookList = ParseBooks(tableNod, bookList);
                }

                // Accounts
                if (tableNod.Name == "table" && tableNod.Attributes["name"].Value == "categories")
                {
                    accountList = ParseAccounts(tableNod, accountList);
                }

                // Transactions
                if (tableNod.Name == "table" && tableNod.Attributes["name"].Value == "transactions")
                {
                    transactionList = ParseTransactions(tableNod, transactionList);
                }
            }

            data[0] = currencyList;
            data[1] = bookList;
            data[2] = accountList;
            data[3] = transactionList;

            // Report amount of account and transaction imported
            Console.WriteLine(currencyList.Count + " currencies, " + bookList.Count + " books, " + accountList.Count + " accounts and " + transactionList.Count + " transactions imported.\n");
            return data;
        }
        public static List<Currency> ParseCurrencies(XmlNode nod, List<Currency> currencyList)
        // Return a list of "Currency" objects,
        // which is built from the list "currenyList" (supplied as argument) and
        // the new "Currency" objects created from the currencies found
        // in the XmlNode "nod" (supplied as argument).
        {
            Console.WriteLine("\"Currency\" table found. Importing accounts...");

            // Run through all nodes within "accounts" table
            int iCur = 0;
            foreach (XmlNode rowCurNod in nod)
            {
                // create a Currency object
                Currency cur = new Currency();

                // Set "Currency object" attribute values
                foreach (XmlNode colCurNod in rowCurNod)
                {
                    switch (colCurNod.Attributes["name"].Value)
                    {
                        case "cur_id":
                            cur.currencyId = colCurNod.InnerText;
                            break;

                        case "cur_code":
                            cur.currencyCode = colCurNod.InnerText;
                            break;

                        case "cur_name":
                            cur.currencyName = colCurNod.InnerText;
                            break;
                    }
                }

                // add cat it to the category list
                currencyList.Add(cur);
                // Clear "cat" variable
                cur = null;

                iCur++;
            }
            return currencyList;
        }
        public static List<Book> ParseBooks(XmlNode nod, List<Book> bookList)
        // Return a list of "Book" objects,
        // which is built from the list "bookList" (supplied as argument) and
        // the new "Book" objects created from the books found
        // in the XmlNode "nod" (supplied as argument).
        {
            Console.WriteLine("\"Book\" table found. Importing accounts...");

            // Run through all nodes within "accounts" table
            int iBoo = 0;
            foreach (XmlNode rowBooNod in nod)
            {
                // create a Book object
                Book boo = new Book();

                // Set "Book object" attribute values
                foreach (XmlNode colBooNod in rowBooNod)
                {
                    switch (colBooNod.Attributes["name"].Value)
                    {
                        case "id":
                            boo.bookId = colBooNod.InnerText;
                            break;

                        case "name":
                            boo.bookName = colBooNod.InnerText;
                            break;

                        case "cur_id":
                            boo.bookCurrency = colBooNod.InnerText;
                            break;
                    }
                }

                // add cat it to the category list
                bookList.Add(boo);
                // Clear "cat" variable
                boo = null;

                iBoo++;
            }
            return bookList;
        }
        public static List<Account> ParseAccounts(XmlNode nod, List<Account> accountList)
        // Return a list of "Account" objects,
        // which is built from the list "accountList" (supplied as argument) and
        // the new "Account" objects created from the accounts found
        // in the XmlNode "nod" (supplied as argument).
        {
            Console.WriteLine("\"Categories\" table found. Importing categories...");

            // Run through all nodes within "categories" table
            int iCat = 0;
            foreach (XmlNode rowAccNod in nod)
            {
                // create a Account object
                Account cat = new Account();

                // Set "Account object" attribute values
                foreach (XmlNode colAccNod in rowAccNod)
                {
                    switch (colAccNod.Attributes["name"].Value)
                    {
                        case "cat_id":
                            cat.accountId = colAccNod.InnerText;
                            break;

                        case "cat_name":
                            cat.accountName = colAccNod.InnerText;
                            break;

                        case "cat_type":
                            cat.accountType = colAccNod.InnerText;
                            break;

                        case "account_id":
                            cat.accountBookID = Convert.ToInt16(colAccNod.InnerText);
                            break;

                        case "parent_id":
                            cat.accountParentAccountID = Convert.ToInt16(colAccNod.InnerText);
                            break;
                    }
                }

                // add cat it to the category list
                accountList.Add(cat);

                // Clear "cat" variable
                cat = null;

                iCat++;
            }
            return accountList;
        }
        public static List<Transaction> ParseTransactions(XmlNode nod, List<Transaction> transactionList)
        // Return a list of "Transaction" objects,
        // which is built from the list "transactionList" (supplied as argument) and
        // the new "Transaction" objects created from the accounts found
        // in the XmlNode "nod" (supplied as argument).
        {

            Console.WriteLine("Transaction table found. Importing transactions...");

            // Run through all nodes within "transactions" table
            foreach (XmlNode rowTraNod in nod)
            {
                // create a Transaction object
                Transaction tra = new Transaction();

                // Set "Transaction object" attribute values
                foreach (XmlNode colTraNod in rowTraNod)
                {
                    switch (colTraNod.Attributes["name"].Value)
                    {
                        case "account_id": // Transaction account (MoneyLover) or book (GNUcash)
                            tra.book = colTraNod.InnerText;
                            break;

                        case "cat_id": // Transaction category ID (MoneyLover) or account (GNUcash)
                            tra.account = colTraNod.InnerText;
                            break;

                        case "display_date": // Transaction display date (MoneyLover) or date (GNUcash)
                            tra.date = Convert.ToDateTime(colTraNod.InnerText);
                            break;

                        case "amount": // Transaction amount
                            tra.amount = Convert.ToDouble(colTraNod.InnerText);
                            break;

                        case "id": // Transaction ID (MoneyLover)
                            tra.id = Convert.ToInt16(colTraNod.InnerText);
                            break;

                        case "note": // Transaction note (MoneyLover) or description (GNUcash)
                            tra.note = colTraNod.InnerText;
                            break;
                    }
                }
                transactionList.Add(tra);   // Add cat it to the transaction list
                tra = null;                 // Clear "tra" variable
            }
            return transactionList;
        }



        // -----------------------------------------------------------------------------------------------------
        //
        //      Data formating methods
        //
        // -----------------------------------------------------------------------------------------------------
        public static object[] FormatData(object[] data, List<AccountDictionaryEntry> accountDictionary)
        // Return the provided data formated properly,
        // that is, with the correct account name, account type,
        // account sign (+ deposit, - withdrawal), etc.
        {
            int i = 0;

            // Unwrap data
            List<Currency> currencyList = data[0] as List<Currency>;
            List<Book> bookList = data[1] as List<Book>;
            List<Account> accountList = data[2] as List<Account>;
            List<Transaction> transactionList = data[3] as List<Transaction>;

            // New transactionList
            List<Transaction> formatedTransactionList = new List<Transaction>();
            
            // Run through each transaction within transactionList list
            foreach (Transaction tra in transactionList)
            {
                Transaction formatedTransaction = FormatTransactions(tra, accountList, accountDictionary, bookList);
                formatedTransactionList.Add(formatedTransaction);
                formatedTransaction = null;
                i++;
            }

            // Wrap data and return it
            object[] formatedData = new object[4];
            formatedData[0] = currencyList;
            formatedData[1] = bookList;
            formatedData[2] = accountList;
            formatedData[3] = formatedTransactionList;
            return formatedData;
        }
        public static Transaction FormatTransactions(Transaction transaction, List<Account> accountList, List<AccountDictionaryEntry> accountDictionary, List<Book> bookList)
        // Return the provided transaction formated properly,
        // that is, with the correct account name, account type,
        // account sign (+ deposit, - withdrawal), etc.
        {
            Transaction formatedTransaction = new Transaction();
            formatedTransaction = transaction;

            // Update account type
            transaction.accountType = GetMoneyLoverAccountType(transaction.account, accountList);
            switch (transaction.accountType)
            {
                case "1":
                    formatedTransaction.amount = Math.Abs(transaction.amount);
                    break;
                case "2":
                    formatedTransaction.amount = -Math.Abs(transaction.amount);
                    break;
            }
            formatedTransaction.accountType = "Bank";


            // Update account name
            if (GetMoneyLoverBookName(transaction.book, bookList) != "unknown")
            {
                formatedTransaction.account = GetGnuCashAccount(accountDictionary, GetMoneyLoverBookName(transaction.book, bookList), GetMoneyLoverAccountName(transaction.account, accountList));
            }

            // Update amount sign (+/-) --------------------------------------- TODO

            // Update account type (Cash, Bank... ) --------------------------- TODO

            return formatedTransaction;
        }


        // -----------------------------------------------------------------------------------------------------
        //
        //      Exporting formated data
        //
        // -----------------------------------------------------------------------------------------------------
        public static void ExportToQif(List<Transaction> transactionList, string outFilePath)
        // Export the supplied string into a QIF file
        {
            // Allow user to select a path (take it from an argument at Main() )

            // Create a file (if exists, overwrite)
            //System.IO.File.Create(outFilePath);
            //Console.WriteLine("\nFile \"" + outFilePath + "\" created.");

            string transactionListAsStringQIF = "";

            foreach (Transaction tra in transactionList)
            {
                transactionListAsStringQIF = transactionListAsStringQIF + tra.ToStringQIF();
            }

            System.IO.File.WriteAllText(outFilePath, transactionListAsStringQIF);

            Console.WriteLine("\nFile \"" + outFilePath + "\" created.");
        }
    }

    public class Currency
    {
        // Attributes
        public string currencyId    { get; set; }
        public string currencyCode  { get; set; }
        public string currencyName  { get; set; }

        // Default contructor
        public Currency()
        {
            currencyId = "unknown";
            currencyCode = "unknown";
            currencyName = "unknown";
        }
    }
    public class Book
    {
        // Attributes
        public string bookId { get; set; }
        public string bookName { get; set; }
        public string bookCurrency { get; set; }

        // Default contructor
        public Book()
        {
            bookId = "unknown";
            bookName = "unknown";
            bookCurrency = "unknown";
        }
    }
    public class Account
    {
        // Attributes
        public string   accountId               { get; set; }
        public string   accountName             { get; set; }
        public string   accountType             { get; set; }
        public int      accountBookID           { get; set; }
        public int      accountParentAccountID  { get; set; }

        // Default constructor
        public Account()
        {
            accountId = "unknown";          // Account ID
            accountName = "unknown";        // Account name
            accountType = "0";              // Account type (1 income, 2 expense)
            accountBookID = 0;              // Book ID (Money Lover account)
            accountParentAccountID = 0;     // Parent account ID (Money Lover parent category)
        }
    }
    public class Transaction
    {
        // Attributes
        public string       book        { get; set; }
        public string       account     { get; set; }
        public DateTime     date        { get; set; }
        public double       amount      { get; set; }
        public int          id          { get; set; }
        public string       note        { get; set; }
        public string       accountType { get; set; } // QIF: Bank, Cash, Expense...

        // Default constructor
        // TODO: test this constructor ----------------------------------------- TEST
        public Transaction()
        {
            book = "unknown";
            account = "unknown";
            date = DateTime.Parse("1900-01-01");
            amount = 0;
            id = 0;
            note = null;
            accountType = "unknown";
        }
        // TODO: test this constructor ----------------------------------------- TEST
        public Transaction(string tb, string tac, DateTime td, double tam, int ti, string tn, string tat)
        {
            book = tb;
            account = tac;
            date = td;
            amount = tam;
            id = ti;
            note = tn;
            accountType = tat;
        }
        public string ToStringQIF()
        // Return Transaction object information as a string, QIF formatted
        {
            string txt =
                "!Type:" + this.accountType + 
                "\nD" + this.date.ToString("dd / MM / yyyy") +
                "\nT" + this.amount.ToString("F2") + // positive or negative symbol should be already evaluated before this point ------------- TODO
                "\nM" + this.note +
                "\nL" + this.account +
                "\n^\n"
                ;
            return txt;
        }
    }
    public class AccountDictionaryEntry
    {
        // Attributes
        public string gnucashAccountName    { get; set; }
        public string moneyloverBook        { get; set; }
        public string moneyloverCategory    { get; set; }
    }
}
