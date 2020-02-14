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

        public async Task<int> CreateCarisProject(string projectName, string creatorHpdUsername,
            List<string> assignedHpdUsernames, string projectType, string projectStatus, string projectPriority, int carisTimeout)
        {
            var projectId = 0;

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

            // Create project


            return projectId;

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

        private async Task CreateCarisProject(int userId, int projectId, int projectTypeId,
    int statusId, int priortyId, int carisTimeout)
        {
            using (var command = _hpdDbContext.Database.GetDbConnection().CreateCommand())
            {

                command.CommandTimeout = carisTimeout;

                //var projectCommand = "DECLARE " +
                //                     "v_project_id integer; " +
                //                     $"v_created_by hpdowner.project.created_by%type := {userId}; " +
                //                     $"v_project_name hpdowner.project.pj_name%type := '{projectId}'; " +
                //                     "v_work_order hpdowner.project.pj_work_order%type := ''; " +
                //                     "v_start_date hpdowner.project.pjdate_started%type := sysdate; " +
                //                     "v_process_time hpdowner.project.pj_planned_process_time%type := ''; " +
                //                     $"v_type_id hpdowner.project.pte_project_type_id%type := {projectTypeId}; " +
                //                     $"v_status_id hpdowner.project_certification.project_status_id%type := {statusId}; " +
                //                     $"v_priority_id hpdowner.project.spy_priority_id%type := {priortyId}; " +
                //                     "v_geom hpdowner.project.geom%type := NULL; " +
                //                     "v_external_id hpdowner.project.external_id%type := NULL; " +
                //                     $"v_assigned_user1 CONSTANT hpdowner.hydrodbusers.HYDRODBUSERS_ID%TYPE := {userId}; " +
                //                     "v_assigned_users hpdowner.hpdnumber$table_type := hpdowner.hpdnumber$table_type(); " +
                //                     "v_default_usage hpdowner.usage.usage_id%type := NULL; " +
                //                     "BEGIN " +
                //                     "v_assigned_users.extend(1); " +
                //                     "v_assigned_users(1) := hpdowner.hpdnumber$row_type(v_assigned_user1); " +
                //                     "v_project_id := hpdowner.p_project_manager.addproject( " +
                //                     "v_created_by, v_project_name, v_work_order, " +
                //                     "v_start_date, v_process_time, " +
                //                     "v_type_id, v_status_id, " +
                //                     "v_priority_id, v_geom, " +
                //                     "v_external_id, NULL, " +
                //                     "v_assigned_users, v_default_usage); " +
                //                     "END; ";

                command.CommandText = "hpdowner.p_project_manager.addproject";
                command.Parameters.Add(new OracleParameter("v_project_id", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_created_by", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_project_name", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_work_order", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_start_date", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_process_time", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_type_id", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_status_id", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_priority_id", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_geom", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_external_id", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_assigned_user1", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_assigned_users", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("v_default_usage", OracleDbType.Int32).Value = 123);
                command.Parameters.Add(new OracleParameter("p_LTSTNM", OracleDbType.Varchar2)).Value = nmNumber;
                //command.BindByName = true;
                command.CommandType = CommandType.StoredProcedure;
                command.ExecuteNonQuery();


                //command.CommandText =
                //    "SELECT hpdowner.p_project_manager.addproject(213321, 'asds', '', NULL, '12:00', 132123, 13223, 12321, NULL, NULL, NULL, NULL, NULL) FROM dual; ";
                //command.ExecuteNonQuery();
            }
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
