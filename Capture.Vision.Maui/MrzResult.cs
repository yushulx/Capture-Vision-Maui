namespace Capture.Vision.Maui
{
    public class Line
    {
        public int Confidence { get; set; }

        public string Text { get; set; }

        public int[] Points { get; set; }
    }

    public class MrzResult
    {
        public string Type { get; set; } = "N/A";
        public string Nationality { get; set; } = "N/A";
        public string Surname { get; set; } = "N/A";
        public string GivenName { get; set; } = "N/A";
        public string PassportNumber { get; set; } = "N/A";
        public string IssuingCountry { get; set; } = "N/A";
        public string BirthDate { get; set; } = "N/A";
        public string Gender { get; set; } = "N/A";
        public string Expiration { get; set; } = "N/A";
        public string Lines { get; set; } = "N/A";

        public Line[] RawData { get; set; }

        // Constructor
        public MrzResult(
            string type = "N/A",
            string nationality = "N/A",
            string surname = "N/A",
            string givenName = "N/A",
            string passportNumber = "N/A",
            string issuingCountry = "N/A",
            string birthDate = "N/A",
            string gender = "N/A",
            string expiration = "N/A",
            string lines = "N/A")
        {
            Type = type;
            Nationality = nationality;
            Surname = surname;
            GivenName = givenName;
            PassportNumber = passportNumber;
            IssuingCountry = issuingCountry;
            BirthDate = birthDate;
            Gender = gender;
            Expiration = expiration;
            Lines = lines;
        }

        // ToString Method
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Type)) return "No results";

            return $"Type: {Type}\n\n" +
                   $"Nationality: {Nationality}\n\n" +
                   $"Surname: {Surname}\n\n" +
                   $"Given name: {GivenName}\n\n" +
                   $"Passport Number: {PassportNumber}\n\n" +
                   $"Issue Country: {IssuingCountry}\n\n" +
                   $"Date of birth: {BirthDate}\n\n" +
                   $"Gender: {Gender}\n\n" +
                   $"Expiration: {Expiration}\n\n" + $"Lines: {Lines}\n\n";
        }

        // ToJson Method
        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
        {
            { "type", Type ?? "" },
            { "nationality", Nationality ?? "" },
            { "surname", Surname ?? "" },
            { "givenName", GivenName ?? "" },
            { "passportNumber", PassportNumber ?? "" },
            { "issuingCountry", IssuingCountry ?? "" },
            { "birthDate", BirthDate ?? "" },
            { "gender", Gender ?? "" },
            { "expiration", Expiration ?? "" },
            { "lines", Lines }
        };
        }

        // FromJson Factory Method
        public static MrzResult FromJson(Dictionary<string, string> json)
        {
            return new MrzResult(
                json.ContainsKey("type") ? json["type"].ToString() : "N/A",
                json.ContainsKey("nationality") ? json["nationality"].ToString() : "N/A",
                json.ContainsKey("surname") ? json["surname"].ToString() : "N/A",
                json.ContainsKey("givenName") ? json["givenName"].ToString() : "N/A",
                json.ContainsKey("passportNumber") ? json["passportNumber"].ToString() : "N/A",
                json.ContainsKey("issuingCountry") ? json["issuingCountry"].ToString() : "N/A",
                json.ContainsKey("birthDate") ? json["birthDate"].ToString() : "N/A",
                json.ContainsKey("gender") ? json["gender"].ToString() : "N/A",
                json.ContainsKey("expiration") ? json["expiration"].ToString() : "N/A",
                json.ContainsKey("lines") ? json["lines"].ToString() : "N/A"
            );
        }

    }
}
