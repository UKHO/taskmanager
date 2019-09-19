namespace SourceDocumentCoordinator.Models
{
    public class ContentServiceResponse
    {
        public string Title { get; set; }
        public ResponseProperties Properties { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }

        public class Data
        {
            public string Format { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }

        public class MetaData
        {
            public string Format { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }

        public class ResponseProperties
        {
            public Data Data { get; set; }
            public MetaData MetaData { get; set; }
        }
    }
}
