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

        public async Task<bool> CreateCarisProject(string projectName, string creatorHpdUsername, List<string> assignedToHpdUsernames, string projectType, string projectStatus, string projectPriority)
        {
            // Check if project already exists
            if ( await _hpdDbContext.CarisProjectData.AnyAsync(p => p.ProjectName.Equals(projectName, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException($"Failed to create Caris project {projectName}, project already exists",nameof(projectName));
            }

            // Get Project Creator Id
            var creator = await _hpdDbContext.CarisUsers.SingleOrDefaultAsync(u =>
                u.Username.Equals(creatorHpdUsername, StringComparison.InvariantCultureIgnoreCase));

            if (creator == null)
            {
                throw new ArgumentException($"Failed to get caris creator username {creatorHpdUsername}, user might not exists in HPD", nameof(creatorHpdUsername));
            }

            var creatorId = creator.UserId;

            // Get Assigned To ids
            var assignedToIds = new List<int>(assignedToHpdUsernames.Count);
            foreach (var assignedToHpdUsername in assignedToHpdUsernames)
            {
                var user = await _hpdDbContext.CarisUsers.SingleOrDefaultAsync(u =>
                    u.Username.Equals(assignedToHpdUsername, StringComparison.InvariantCultureIgnoreCase));

                if (user == null)
                {
                    throw new ArgumentException($"Failed to get caris assigned to username {assignedToHpdUsername}, user might not exists in HPD", nameof(assignedToHpdUsernames));
                }

                assignedToIds.Add(user.UserId);
            }

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
    }
