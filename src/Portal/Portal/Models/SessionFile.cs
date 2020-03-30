using System.Collections.Generic;
using System.Xml.Serialization;

namespace Portal.Models
{
    // TODO: Ensure node is called CARIS_Workspace version="1.1"
    [XmlRoot(ElementName = "CARIS_Workspace")]
    public class SessionFile
    {
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }

        [XmlElement(Order = 1, ElementName = "DataSources")]
        public DataSourcesNode DataSources { get; set; }

        [XmlElement(Order = 2, ElementName = "Views")]
        public ViewsNode Views { get; set; }

        [XmlElement(Order = 3, ElementName = "Properties")]
        public PropertiesNode Properties { get; set; }
        
        public SessionFile()
        {

        }

        [XmlRoot(ElementName = "SELECTEDPROJECTUSAGES")]
        public class SelectedProjectUsages
        {
            [XmlElement(ElementName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "SourceParam")]
        public class SourceParamNode
        {
            [XmlElement(ElementName = "SERVICENAME")]
            public string SERVICENAME { get; set; }
            [XmlElement(ElementName = "USERNAME")]
            public string USERNAME { get; set; }
            [XmlElement(ElementName = "ASSIGNED_USER")]
            public string ASSIGNED_USER { get; set; }
            [XmlElement(ElementName = "USAGE")]
            public string USAGE { get; set; }
            [XmlElement(ElementName = "WORKSPACE")]
            public string WORKSPACE { get; set; }
            [XmlElement(ElementName = "SecureCredentialPlugin")]
            public string SecureCredentialPlugin { get; set; }
            [XmlElement(ElementName = "SecureCredentialPlugin_UserParam")]
            public string SecureCredentialPlugin_UserParam { get; set; }
            [XmlElement(ElementName = "HAS_BOUNDARY")]
            public string HAS_BOUNDARY { get; set; }
            [XmlElement(ElementName = "OPENED_BY_PROJECT")]
            public string OPENED_BY_PROJECT { get; set; }
            [XmlElement(ElementName = "PROJECT")]
            public string PROJECT { get; set; }
            [XmlElement(ElementName = "PROJECT_ID")]
            public string PROJECT_ID { get; set; }
            [XmlElement(ElementName = "_PROJECT_BOUNDARIES")]
            public string _PROJECT_BOUNDARIES { get; set; }
            [XmlElement(ElementName = "SELECTEDPROJECTUSAGES")]
            public SelectedProjectUsages SELECTEDPROJECTUSAGES { get; set; }
        }

        [XmlRoot(ElementName = "DataSource")]
        public class DataSourceNode
        {
            [XmlElement(ElementName = "SourceString")]
            public string SourceString { get; set; }
            [XmlElement(ElementName = "SourceParam")]
            public SourceParamNode SourceParam { get; set; }
            [XmlElement(ElementName = "UserLayers")]
            public string UserLayers { get; set; }
        }

        [XmlRoot(ElementName = "DataSources")]
        public class DataSourcesNode
        {
            [XmlElement(ElementName = "DataSource")]
            public DataSourceNode DataSource { get; set; }
        }

        [XmlRoot(ElementName = "displayLayer")]
        public class DisplayLayerNode
        {
            [XmlElement(ElementName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "visible")]
            public string Visible { get; set; }
            [XmlAttribute(AttributeName = "expanded")]
            public string Expanded { get; set; }
        }

        [XmlRoot(ElementName = "DisplayState")]
        public class DisplayStateNode
        {
            [XmlElement(ElementName = "displayLayer")]
            public DisplayLayerNode DisplayLayer { get; set; }
        }

        [XmlRoot(ElementName = "View")]
        public class ViewNode
        {
            [XmlElement(ElementName = "DisplayState")]
            public DisplayStateNode DisplayState { get; set; }
        }

        [XmlRoot(ElementName = "Views")]
        public class ViewsNode
        {
            [XmlElement(ElementName = "View")]
            public ViewNode View { get; set; }
        }

        [XmlRoot(ElementName = "Item")]
        public class ItemNode
        {
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "group")]
            public string Group { get; set; }
            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "Property")]
        public class PropertyNode
        {
            [XmlElement(ElementName = "Item")]
            public ItemNode Item { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }

            [XmlElement(ElementName = "Property")]
            public PropertyNode Property { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string Source { get; set; }
        }

        [XmlRoot(ElementName = "Properties")]
        public class PropertiesNode
        {
            [XmlElement(ElementName = "Property")]
            public List<PropertyNode> Property { get; set; }
        }
    }
}