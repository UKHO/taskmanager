using System.Collections.Generic;
using System.Xml.Serialization;

namespace Portal.Models
{
    public class SessionFile
    {
        public SELECTEDPROJECTUSAGES SelectedProjectUsages = new SELECTEDPROJECTUSAGES();
        public SourceParam SourceParamProp = new SourceParam();
        public CARIS_Workspace CarisWorkspace = new CARIS_Workspace();
        public DataSource DataSourceProp = new DataSource();

        public SessionFile()
        {
            
        }

        [XmlRoot(ElementName = "SELECTEDPROJECTUSAGES")]
        public class SELECTEDPROJECTUSAGES
        {
            [XmlElement(ElementName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "SourceParam")]
        public class SourceParam
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
            public SELECTEDPROJECTUSAGES SELECTEDPROJECTUSAGES { get; set; }
        }

        [XmlRoot(ElementName = "DataSource")]
        public class DataSource
        {
            [XmlElement(ElementName = "SourceString")]
            public string SourceString { get; set; }
            [XmlElement(ElementName = "SourceParam")]
            public SourceParam SourceParam { get; set; }
            [XmlElement(ElementName = "UserLayers")]
            public string UserLayers { get; set; }
        }

        [XmlRoot(ElementName = "DataSources")]
        public class DataSources
        {
            [XmlElement(ElementName = "DataSource")]
            public DataSource DataSource { get; set; }
        }

        [XmlRoot(ElementName = "displayLayer")]
        public class DisplayLayer
        {
            [XmlElement(ElementName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "visible")]
            public string Visible { get; set; }
            [XmlAttribute(AttributeName = "expanded")]
            public string Expanded { get; set; }
        }

        [XmlRoot(ElementName = "DisplayState")]
        public class DisplayState
        {
            [XmlElement(ElementName = "displayLayer")]
            public DisplayLayer DisplayLayer { get; set; }
        }

        [XmlRoot(ElementName = "View")]
        public class View
        {
            [XmlElement(ElementName = "DisplayState")]
            public DisplayState DisplayState { get; set; }
        }

        [XmlRoot(ElementName = "Views")]
        public class Views
        {
            [XmlElement(ElementName = "View")]
            public View View { get; set; }
        }

        [XmlRoot(ElementName = "Item")]
        public class Item
        {
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "group")]
            public string Group { get; set; }
            [XmlAttribute(AttributeName = "value")]
            public string Value { get; set; }
        }

        [XmlRoot(ElementName = "Property")]
        public class Property
        {
            [XmlElement(ElementName = "Item")]
            public Item Item { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }
            
            [XmlElement(ElementName = "Property")]
            // TODO: The property name here needs to be "Property", and not "PropertyTest"
            public Property PropertyTest { get; set; }

            [XmlAttribute(AttributeName = "source")]
            public string Source { get; set; }
        }

        [XmlRoot(ElementName = "Properties")]
        public class Properties
        {
            [XmlElement(ElementName = "Property")]
            public List<Property> Property { get; set; }
        }

        [XmlRoot(ElementName = "CARIS_Workspace")]
        public class CARIS_Workspace
        {
            [XmlElement(ElementName = "DataSources")]
            public DataSources DataSources { get; set; }
            [XmlElement(ElementName = "Views")]
            public Views Views { get; set; }
            [XmlElement(ElementName = "Properties")]
            public Properties Properties { get; set; }
            [XmlAttribute(AttributeName = "version")]
            public string Version { get; set; }
        }
    }
}