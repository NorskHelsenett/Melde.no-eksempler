using Example.Configuration;
using MeldeV2;
using OpenAPI;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Example.AdverseIncident;

class Program
{
    static AttachmentPart[] attachments =
    {
        new ()
        {
            Name = "Test.txt",
            ContentType = "text/plain",
            Content = "SGVyIGVyIGV0IGVua2VsdCB0ZXN0dmVkbGVnZy4NCg=="
        },
        new ()
        {
            Name = "lite-bilde.jpg",
            ContentType= "image/jpeg",
            Content = "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"
        }
    };

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
                    Organization = new OrganizationPart
                    {
                        OrgNumber = "883974832",
                        LocalReferenceNumber = "Local ref text"
                    },
                    Role = ReporterRole.Treator,
                    Position = "Lege"
                },
                Incident = new AdverseIncidentIncidentPart
                {
                    IncidentDescription = "Datt på rattata",
                    IncidentDate = "2021-07-13",
                    DateUnknown = YesNo.No,
                },
                //Patient = new AdverseIncidentPatientPart
                //{
                //    //DateOfBirth = "1990-07-13",
                //    //Gender = Gender.Male,
                //    Nin = "13075706604"
                //},
                ReportAreas = new AdverseIncidentReportAreasPart
                {
                    DrugSideEffects = false,
                    MedicalEquipment = true,
                    SeriousIncident = false,
                    DietarySupplements = false,
                    Biovigilance = false,
                    Cosmetics = false,
                    ECigarette = false,
                    Radiation = false
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
                },
                //Attachments = attachments.Select(a => new AttachmentPart() { Name = a.Name, ContentType = a.ContentType, Content = null}).ToArray()
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
                                BatchLotNumber = "Batch"
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
                },
                DrugSideEffects = new DrugSideEffectsReportPart
                {
                    Drugs = [
                        new DrugSideEffectsDrugPart
                        {
                            // Use active ingredient instead of specific drug
                            DrugId = "ID_DF40453A-DAD1-450D-A58D-0FE411DCDB05",
                            IsActiveIngredient = YesNo.Yes
                        }
                    ],
                    Symptoms = [
                        new DrugSideEffectsSymptomsPart
                        {
                             Description = "Haupin"
                        }
                    ],
                    OtherInformation = new DrugSideEffectsOtherInformationPart
                    {
                        HospitalName = "Sykehuset"
                    }
                },
                MedicalEquipment = new()
                {
                    EquipmentName = "Name of the equipment",
                    Manufacturer = "Manufacturer of the equipment",
                    NkknCategory = NkknCategory.ElectroMechanicEquipment,
                    EquipmentLocation = EquipmentLocation.Healthcare,
                    IncidentClassification = IncidentClassification.MayHaveCausedDeathOrSeriousDeterioration,
                }
            }
        };

        // Example of data sent with request
        var jsonData = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(jsonData);
        Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}-------------------------------------------------{Environment.NewLine}{Environment.NewLine}");

        try
        {
            //call API, wait for response
            var httpClient = CreateClient(["nhn:melde/report/send"]);
            var apiClient = new Client(httpClient);

            // Upload form and files
            using var content = new MultipartFormDataContent();

            content.Add(
                content: JsonContent.Create(requestData, options: new JsonSerializerOptions { PropertyNamingPolicy = null }),
                name: "adverseIncidentRequest"
            );

            foreach(var attachment in attachments)
            {
                var fileStream = new MemoryStream(Convert.FromBase64String(attachment.Content));
                content.Add(
                    content: new StreamContent(fileStream).WithContentType(attachment.ContentType),
                    name: "attachments",              // MUST match parameter name
                    fileName: attachment.Name
                );
            }

            var response2 = await httpClient.PostAsync($"api/v2/report/adverse-incident-multipart", content);
            var respCont = await response2.Content.ReadAsStringAsync();
        }
        catch (ApiException ex)
        {
            Console.WriteLine("-- Feil");
            Console.WriteLine($"HTTP statuskode: {ex.StatusCode}");
            Console.WriteLine($"Feilemdling: {ex.Message}");
        }
    }

    private static HttpClient CreateClient(string[] scopes)
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

        var jwtHandler = new JwkTokenHandler(Config.HelseIdUrl, Config.ClientId, Config.Jwk, scopes, htHandler);

        var httpClient = new HttpClient(jwtHandler)
        {
            BaseAddress = Config.ApiUri,
        };

        return httpClient;
    }
}

public static class StreamContentExtension
{
    public static StreamContent WithContentType(this StreamContent content, string contentType)
    {
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }
}
