using System;

namespace DataServices.Models
{
    public class LinkedDocumentMetadata
    {
        public class Document
        {
            public int IdField { get; set; }
            public string NameField { get; set; }
            public int DocumentTypeIdField { get; set; }
            public string SourceNameField { get; set; }
            public string SpatialExtentPolygonField { get; set; }
            public int CRsIdField { get; set; }
            public string CRsParamsField { get; set; }
            public int DTIdField { get; set; }
            public string DTNameField { get; set; }
            public int DTFromDatumField { get; set; }
            public string DTMethodField { get; set; }
            public double DTDxField { get; set; }
            public double DTDyField { get; set; }
            public double DTDzField { get; set; }
            public double DTRxField { get; set; }
            public double DTRyField { get; set; }
            public double DTRzField { get; set; }
            public double DTScaleField { get; set; }
            public double DTAccuracyField { get; set; }
            public string DTSourceFileField { get; set; }
            public string DTStatusField { get; set; }
            public string SepNameField { get; set; }
            public int ScaleField { get; set; }
            public double SpatialReferencingAccuracyField { get; set; }
            public int StatusField { get; set; }
            public DateTime RegistrationDateField { get; set; }
        }
    }
}
