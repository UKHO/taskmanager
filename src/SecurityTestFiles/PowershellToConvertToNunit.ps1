$XslPath = "/tmp/nunit/OWASPToNUnit3.xslt"
$XmlInputPath = "/tmp/report/report.xml"
$XmlOutputPath = "/tmp/report/Converted-OWASP-ZAP-Report.xml"
$XslTransform = New-Object System.Xml.Xsl.XslCompiledTransform
$XslTransform.Load($XslPath)
$XslTransform.Transform($XmlInputPath,$XmlOutputPath)