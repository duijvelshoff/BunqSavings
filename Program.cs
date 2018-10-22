using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Bunq.Sdk.Context;
using Bunq.Sdk.Model.Generated.Endpoint;
using Bunq.Sdk.Model.Generated.Object;

namespace bunqJob
{
    class Program
    {
        static void Main(string[] args)
        {
            JObject config = JObject.Parse(File.ReadAllText(@"appsettings.json"));

            if (!(File.Exists(@"bunq.conf")))
            {
                string apiKey = null;
                ApiContext apiContextSetup;
                if ((config["bunq"]["apikey"].ToString()).Length >= 1)
                {
                    apiKey = (string)config["bunq"]["apikey"];
                }
                if (Environment.GetEnvironmentVariable("INIT") != "false")
                {
                    Console.WriteLine("Whoops, there is no API key defined yet! If this is the first run, please supply your API key and press Enter.");
                    apiKey = Console.ReadLine();
                }
                if (apiKey != null)
                {
                    apiContextSetup = ApiContext.Create(ApiEnvironmentType.PRODUCTION, apiKey, "bunqJob");
                    apiContextSetup.Save();
                }
                else
                {
                    return;
                }
            }         

            Console.WriteLine("Hi there, we are connecting to the bunq API...\n");
            
            var apiContext = ApiContext.Restore();
            BunqContext.LoadApiContext(apiContext);
            Console.WriteLine(" -- Connected as: " + BunqContext.UserContext.UserPerson.DisplayName + " (" + BunqContext.UserContext.UserId + ")\n");
            
            Console.WriteLine("Let's start transfering money!\n");
            
            foreach (JObject rule in config["rules"]){
                
                var AllMonetaryAccounts = MonetaryAccountBank.List().Value;
                int MonetaryAccountId = 0;
                string MonetaryAccountBalance = null;

                foreach (var MonetaryAccount in AllMonetaryAccounts)
                {
                    foreach (var Alias in MonetaryAccount.Alias)
                    {
                        if (Alias.Value == rule["origin-account"].ToString())
                        {
                            MonetaryAccountId = MonetaryAccount.Id.Value;
                            MonetaryAccountBalance = MonetaryAccount.Balance.Value;
                        }
                    }
                }

                double AmountToTransfer = 0;
                string DefinedTransaction = null;
                string SuggestedTransaction = null;

                switch (rule["amount"]["type"].ToString())
                {
                    case "exact":
                        if (Double.Parse(MonetaryAccountBalance) >= Double.Parse(rule["amount"]["value"].ToString()))
                        {
                            AmountToTransfer = Double.Parse(rule["amount"]["value"].ToString());
                            DefinedTransaction = "€" + rule["amount"]["value"].ToString() + " (Exact amount)";
                            SuggestedTransaction = AmountToTransfer.ToString("0.00") + " - Reason: Due to insufficied balance on the account.";
                        }
                        else 
                        {
                            AmountToTransfer = Double.Parse(MonetaryAccountBalance);
                            DefinedTransaction = "€ " + rule["amount"]["value"].ToString() + " (Exact amount)";
                            SuggestedTransaction = AmountToTransfer.ToString("0.00");
                        }
                    break;
                    case "percent":
                        AmountToTransfer = (Double.Parse(MonetaryAccountBalance) * (Double.Parse(rule["amount"]["value"].ToString())/100));
                        DefinedTransaction = rule["amount"]["value"].ToString() + "% of € " + MonetaryAccountBalance;
                        SuggestedTransaction = AmountToTransfer.ToString("0.00");
                    break;
                    default:
                    break;
                }

                Console.WriteLine("Todo:");
                Console.WriteLine("----------------------------------------------------------");
                Console.WriteLine("Transaction Name:      " + rule["name"].ToString());
                Console.WriteLine("Origin Account:        " + rule["origin-account"].ToString() + " (" + MonetaryAccountId.ToString() + ")");
                Console.WriteLine("Destination Account:   " + rule["destination-account"].ToString());
                Console.WriteLine("Current Balance:       € " + MonetaryAccountBalance);
                Console.WriteLine("Defined Transaction:   " + DefinedTransaction);
                Console.WriteLine("Suggested Transaction: € " + SuggestedTransaction);
                Console.WriteLine("----------------------------------------------------------");

                if(AmountToTransfer > 0)
                {
                    var Recipient = new Pointer("IBAN", rule["destination-account"].ToString());
                    Recipient.Name = BunqContext.UserContext.UserPerson.DisplayName;
                    Console.WriteLine("Executing...");
                    var PaymentID = Payment.Create(new Amount(AmountToTransfer.ToString("0.00"), "EUR"), Recipient, "Transfer to Savings Account", MonetaryAccountId).Value;
                }
                else
                {
                    Console.WriteLine("Skipping...");
                }
                Console.WriteLine("Yeah, this one is completed!");
                Console.WriteLine("----------------------------------------------------------\n");
            }
        Console.WriteLine("And we're done for now!");
        }
    }
}
