
using AddReference = clr.AddReference;

public static class SECReportDataAlgorithm {
    
    static SECReportDataAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class SECReportDataAlgorithm
        : QCAlgorithm {
        
        public object symbol;
        
        public string ticker;
        
        public virtual object Initialize() {
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
            this.SetStartDate(2019, 1, 1);
            this.SetEndDate(2019, 1, 31);
            this.SetCash(100000);
            this.ticker = "AAPL";
            this.symbol = this.AddData(SECReport10Q, this.ticker, Resolution.Daily).Symbol;
            this.AddData(SECReport8K, this.ticker, Resolution.Daily);
        }
        
        public virtual object OnData(object slice) {
            // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
            var data = slice[this.ticker];
            var report = data.Report;
            this.Log("Form Type {report.FormType}");
            this.Log("Filing Date: {str(report.FilingDate)}");
            foreach (var filer in report.Filers) {
                this.Log("Filing company name: {filer.CompanyData.ConformedName}");
                this.Log("Filing company CIK: {filer.CompanyData.Cik}");
                this.Log("Filing company EIN: {filer.CompanyData.IrsNumber}");
                foreach (var formerCompany in filer.FormerCompanies) {
                    this.Log("Former company name of {filer.CompanyData.ConformedName}: {formerCompany.FormerConformedName}");
                    this.Log("Date of company name change: {str(formerCompany.Changed)}");
                }
            }
            // SEC documents can come in multiple documents.
            // For multi-document reports, sometimes the document contents after the first document
            // are files that have a binary format, such as JPG and PDF files
            foreach (var document in report.Documents) {
                this.Log("Filename: {document.Filename}");
                this.Log("Document description: {document.Description}");
                // Print sample of contents contained within the document
                this.Log(document.Text[::100]);
                this.Log("=================");
            }
        }
    }
}
