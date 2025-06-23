using Example.Configuration;
using MeldeV2;
using Newtonsoft.Json;
using OpenAPI;

namespace Example.Varselordningen
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //// Fill out request data
            var requestData = new AdverseIncidentRequest
            {
                Header = new AdverseIncidentHeaderPart
                {
                    ExternalCaseId = Guid.NewGuid().ToString(),
                    Reporter = new AdverseIncidentReporterPart
                    {
                        Nin = "13075706604",
                        Email = "TestData@melde.no",
                        Phone = "99999999",
                        Organization = new OrganizationPart { OrgNumber = "883974832" },
                        Role = ReporterRole.Treator,
                        Position = "Lege"
                    },
                    Incident = new AdverseIncidentIncidentPart
                    {
                        IncidentDescription = "Datt på rattata",
                        IncidentDate = "2021-07-13",
                        DateUnknown = YesNo.No
                    },
                    Patient = new AdverseIncidentPatientPart
                    {
                        //DateOfBirth = "1990-07-13",
                        //Gender = Gender.Male,
                        Nin = "13075706604"
                    },
                    ReportAreas = new AdverseIncidentReportAreasPart
                    {
                        SeriousIncident = true,
                        DietarySupplements = true,
                        Biovigilance = true
                    },
                    ContactPersons = new List<ContactPersonPart>
                    {
                        new ContactPersonPart
                        {
                            Name = "VILDE MOEN_BRATLI",
                            Email = "TestData@melde.no",
                            Phone = "00000000",
                            Position = "Doktor"
                        }
                    }
                },
                Report = new AdverseIncidentReportPart
                {
                    SeriousIncident = new SeriousIncidentReportPart
                    {
                        NextOfKin = new List<SeriousIncidentNextOfKin>
                        {
                            new SeriousIncidentNextOfKin
                            {
                                Name = "Bror Børresen"
                            }
                        }
                    },
                    DietarySupplements = new DietarySupplementsReportPart
                    {
                        SideEffect = new DietarySupplementsSideEffectsPart
                        {
                            Reactions = new List<string>()
                            {
                                "EczemaRash",
                                "Swelling"
                            },
                            ReactionDelay = "ReakTid",
                            ReactionDelayDescription = "ReakTidTekst",
                            Outcome = "Folger",
                            OutcomeDescription = "FolgerTekst",
                            SideEffectDuration = "BivVarighet",
                            Ongoing = YesNo.Yes,
                            SideEffectsOnPreviousUse = YesNoDontKnow.DontKnow,
                            SideEffectsOnPreviousUseDescription = "BivTidBrukTekst",
                            AnyAllergiesOrInfluencingIssues = YesNoDontKnow.Yes,
                            AnyAllergiesOrInfluencingIssuesDescription = "AllergiTekst",
                            AnyUnderlyingDiseases = YesNoDontKnow.Yes,
                            CanDiseaseBeTheCause = YesNoDontKnow.No
                        },
                        Products = new[]
                        {
                            new DietarySupplementsProductPart
                            {
                                ProductInformation = new DietarySupplementsProductInformationPart
                                {
                                    ProductName = "ProdNavn",
                                    Ingredients = "Ingr",
                                    VendorOrManufacturer = "LevProdusent",
                                    BestBeforeDate = "2022-12-02",
                                    ReportedToVendorOrManufacturer = YesNoDontKnow.DontKnow,
                                    PurchaseLocation = "Hvor",
                                    ShopName = "Butikknavn",
                                    BatchLotNumber = "Batch",
                                    Attachments = new []
                                    {
                                        new AttachmentPart
                                        {
                                            Name = "Test.txt",
                                            Content = "SGVyIGVyIGV0IGVua2VsdCB0ZXN0dmVkbGVnZy4NCg=="
                                        }
                                    }
                                },
                                ProductUsage = new DietarySupplementsProductUsagePart
                                {
                                    AmountOfIngredientsInDailyDose = "Mengde",
                                    RecommendedDailyDose = "DglDose",
                                    ActualDailyDoseTaken = "FaktiskDose",
                                    UsageDuration = "Periode",
                                    AnyDrugsTakenAtSameTime = YesNoDontKnow.Yes,
                                    DrugsTakenAtSameTime = new [] { "Dundersalt" },

                                }
                            }
                        }
                    },
                    Biovigilance = new BiovigilanceReportPart
                    {
                        IncidentLocation = "1.1",
                        TypeOfIncident = "2.10.1.1.1",
                        ContributingFactors = new string[] { "3.2.1" },
                        Preventability = "4.1",
                        ActualConsequenceForPatient = "5.2",
                        Frequency = "6.2",
                        PossibleConsequenceOnRepetition = "7.3",
                        DefinitionCode = "3",
                        DiscoveredCode = "2"
                    }
                }
            };

            // Example of data sent with request
            var jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
            Console.WriteLine(jsonData);
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

            try
            {
                //call API, wait for response
                var apiClient = CreateClient(["nhn:melde/report/send"]);
                var response = await apiClient.AdverseIncidentAsync(requestData);

                Console.WriteLine("-- Responsdata");
                Console.WriteLine($"Melde.no ID: {response.Id}");
                Console.WriteLine($"EksterntSaksSystem: {response.EksterntSaksSystem}");
                Console.WriteLine($"EksternSaksid: {response.EksternSaksid}");
                Console.WriteLine($"MeldingsId: {response.MeldingsId}");
                Console.WriteLine($"Referansenummer: {response.Referansenummer}");
                Console.WriteLine($"MeldersEpost: {response.MeldersEpost}");
                Console.WriteLine("-- Mottakere");
                foreach (var m in response.Mottakere)
                {
                    Console.WriteLine($"\t{m}");
                }
            }
            catch(ApiException ex)
            {
                Console.WriteLine("-- Feil");
                Console.WriteLine($"HTTP statuskode: {ex.StatusCode}");
                Console.WriteLine($"Feilemdling: {ex.Message}");
            }
        }

        private static Client CreateClient(string[] scopes)
        {
            var htHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Accept even if the certificate is expired
                    if (errors == System.Net.Security.SslPolicyErrors.None ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors ||
                        errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                    {
                        return true;
                    }

                    return false;
                },

            };

            var jwtHandler = new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, Config.ClientType, Config.TokenType, htHandler);

            var httpClient = new HttpClient(jwtHandler)
            {
                BaseAddress = Config.ApiUri,
            };

            return new Client(httpClient);
        }
    }
}
