using System;
using System.Collections.Generic;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Common.Helpers
{
    public class CarisProjectHelper
    {
        private readonly HpdDbContext _hpdDbContext;

        public CarisProjectHelper(HpdDbContext hpdDbContext)
        {
            _hpdDbContext = hpdDbContext;
        }

        public async Task<bool> CreateCarisProject(string projectName, string creatorHpdUsername,
            List<string> assignedHpdUsernames, string projectType, string projectStatus, string projectPriority)
        {
            // Check if project already exists
            if (await _hpdDbContext.CarisProjectData.AnyAsync(p =>
                p.ProjectName.Equals(projectName, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException($"Failed to create Caris project {projectName}, project already exists",
                    nameof(projectName));
            }

            // Get Project Creator Id
            var creatorUsernameId = await GetHpdUserId(creatorHpdUsername);

            // Get Assigned users ids
            var assignedusersId = await GetAssignedUsersId(assignedHpdUsernames);

            // Get Caris Project Type Id
            var projectTypeId = await GetCarisProjectTypeId(projectType);

            // Get Caris project status Id
            var carisprojectStatusId = await GetCarisProjectStatusId(projectStatus);

            // Get Caris project priority Id
            var carisProjectPriortyId = await GetCarisProjectPriorityId(projectPriority);

            return true;

            //var creatingProjectResponse = new DbResponse<HpdCreateProjectResponse>
            //{
            //    Success = false,
            //    Response = HpdCreateProjectResponse.Failed
            //};

            //using (var connection = new OracleConnection(_hpdConnectionString))
            //{
            //    connection.Open();
            //    var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            //    try
            //    {

            //        var getCarisProjectResponse = GetCarisProjectId(connection, projectId);
            //        if (getCarisProjectResponse > 0)
            //        {
            //            creatingProjectResponse.Message = $"Caris Project {projectId} already exists.";
            //            creatingProjectResponse.Response = HpdCreateProjectResponse.AlreadyExists;
            //            creatingProjectResponse.Success = true;
            //            return creatingProjectResponse;
            //        }
            //        var userId = GetHpdUserId(connection, creatorUsername);
            //        var projectTypeId = GetHpdProjectType(connection, projectType);

            //        var statusId = GetHpdProjectStatus(connection, projectStatus);
            //        var priortyId = GetHpdProjectPriorty(connection, projectPriority);

            //        CreateCarisProject(connection, userId, projectId, projectTypeId, statusId, priortyId);

            //        transaction.Commit();
            //    }
            //    catch (OracleException ex)
            //    {
            //        transaction.Rollback();
            //        creatingProjectResponse.Exception = ex;
            //        creatingProjectResponse.Message = ex.FormatOracleError();
            //        return creatingProjectResponse;
            //    }
            //    finally
            //    {
            //        transaction.Dispose();
            //    }
            //}

            //creatingProjectResponse.Success = true;
            //creatingProjectResponse.Message = $"Caris Project {projectId} created successfully.";
            //creatingProjectResponse.Response = HpdCreateProjectResponse.CreateSuccess;
            //return creatingProjectResponse;

        }

        private async Task<int> GetCarisProjectPriorityId(string projectPriority)
        {
            var carisProjectPriority = await _hpdDbContext.CarisProjectPriorities.SingleOrDefaultAsync(s =>
                s.ProjectPriorityName.Equals(projectPriority, StringComparison.InvariantCultureIgnoreCase));

            if (carisProjectPriority == null)
            {
                throw new ArgumentException(
                    $"Failed to get caris project priority {projectPriority}, project status might not exists in HPD",
                    nameof(projectPriority));
            }

            return carisProjectPriority.ProjectPriorityId;
        }

        private async Task<int> GetCarisProjectStatusId(string projectStatus)
        {
            var carisProjectStatus = await _hpdDbContext.CarisProjectStatuses.SingleOrDefaultAsync(s =>
                s.ProjectStatusName.Equals(projectStatus, StringComparison.InvariantCultureIgnoreCase));

            if (carisProjectStatus == null)
            {
                throw new ArgumentException(
                    $"Failed to get caris project status {projectStatus}, project status might not exists in HPD",
                    nameof(projectStatus));
            }

            return carisProjectStatus.ProjectStatusId;
        }

        private async Task<int> GetCarisProjectTypeId(string projectType)
        {
            var carisProjectType = await _hpdDbContext.CarisProjectTypes.SingleOrDefaultAsync(p =>
                p.ProjectTypeName.Equals(projectType, StringComparison.InvariantCultureIgnoreCase));

            if (carisProjectType == null)
            {
                throw new ArgumentException(
                    $"Failed to get caris project type {projectType}, project type might not exists in HPD",
                    nameof(projectType));
            }

            return carisProjectType.ProjectTypeId;
        }

        private async Task<List<int>> GetAssignedUsersId(List<string> assignedHpdUsernames)
        {
            var assignedUsersIds = new List<int>(assignedHpdUsernames.Count);
            foreach (var assignedHpdUsername in assignedHpdUsernames)
            {
                var id = await GetHpdUserId(assignedHpdUsername);

                assignedUsersIds.Add(id);
            }

            return assignedUsersIds;
        }

        private async Task<int> GetHpdUserId(string hpdUsername)
        {
            var creator = await _hpdDbContext.CarisUsers.SingleOrDefaultAsync(u =>
                u.Username.Equals(hpdUsername, StringComparison.InvariantCultureIgnoreCase));

            if (creator == null)
            {
                throw new ArgumentException(
                    $"Failed to get caris username {hpdUsername}, user might not exists in HPD",
                    nameof(hpdUsername));
            }

            return creator.UserId;
        }
    }
}
