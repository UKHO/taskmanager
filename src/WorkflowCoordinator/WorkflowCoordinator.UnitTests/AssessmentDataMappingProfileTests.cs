using System;
using AutoMapper;
using DataServices.Models;
using NUnit.Framework;
using WorkflowCoordinator.MappingProfiles;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class AssessmentDataMappingProfileTests
    {
        private Mapper _mapper;

        [SetUp]
        public void Setup()
        {
            _mapper = new Mapper(new MapperConfiguration(mc => 
                mc.AddProfile(new AssessmentDataMappingProfile())));
        }

        [Test]
        public void Test_Given_DocumentAssessmentData_When_Mapped_Then_AssessmentData_Is_Populated()
        {
            //Arrange
            var documentAssessmentData = new DocumentAssessmentData()
            {
                SdocId = 1,
                Name = "NAME",
                SDODate = DateTime.Today.AddDays(1),
                Team = "TEAM",
                DocumentType = "DOCUMENT_TYPE",
                DocumentNature = "DOCUMENT_NATURE",
                SourceName = "SOURCE_NAME"
            };

            //Act
            var assessmentData = _mapper.Map<DocumentAssessmentData, AssessmentData>(
                documentAssessmentData);

            //Assert
            Assert.IsNotNull(assessmentData);
            Assert.AreEqual(documentAssessmentData.SdocId, assessmentData.PrimarySdocId);
            Assert.AreEqual(documentAssessmentData.Name, assessmentData.SourceDocumentName);
            Assert.AreEqual(documentAssessmentData.SDODate, assessmentData.ToSdoDate);
            Assert.AreEqual(documentAssessmentData.Team, assessmentData.TeamDistributedTo);
            Assert.AreEqual(documentAssessmentData.DocumentType, assessmentData.SourceDocumentType);
            Assert.AreEqual(documentAssessmentData.DocumentNature, assessmentData.SourceNature);
            Assert.AreEqual(documentAssessmentData.SourceName, assessmentData.RsdraNumber);


        }
    }
}
