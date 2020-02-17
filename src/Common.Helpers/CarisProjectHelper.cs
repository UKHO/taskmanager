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

        public async Task<int> CreateCarisProject(int k2ProcessId, string projectName, string creatorHpdUsername,
            List<string> assignedHpdUsernames, string projectType, string projectStatus, string projectPriority,
            int carisTimeout, string workspace)
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
            var carisProjectStatusId = await GetCarisProjectStatusId(projectStatus);

            // Get Caris project priority Id
            var carisProjectPriortyId = await GetCarisProjectPriorityId(projectPriority);

            // Create project
            var t = await CreateProject(k2ProcessId, creatorUsernameId, projectName, projectTypeId, carisProjectStatusId,
                carisProjectPriortyId, carisTimeout, workspace);

            return projectId;
        }

        private async Task<int> CreateProject(int k2processId, int userId, string projectName, int projectTypeId, int statusId,
            int priortyId, int carisTimeout, string workspace)
        {
            int something;

            using (var command = _hpdDbContext.Database.GetDbConnection().CreateCommand())
            {
                var transaction = _hpdDbContext.Database.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    command.CommandTimeout = carisTimeout;

                    var projectCommand = "DECLARE " +
                                         "v_project_id integer; " +
                                         $"v_created_by hpdowner.project.created_by%type := {userId}; " +
                                         $"v_project_name hpdowner.project.pj_name%type := '{projectName}'; " +
                                         "v_work_order hpdowner.project.pj_work_order%type := NULL; " +
                                         "v_start_date hpdowner.project.pjdate_started%type := sysdate; " +
                                         "v_process_time hpdowner.project.pj_planned_process_time%type := NULL; " +
                                         $"v_type_id hpdowner.project.pte_project_type_id%type := {projectTypeId}; " +
                                         $"v_status_id hpdowner.project_certification.project_status_id%type := {statusId}; " +
                                         $"v_priority_id hpdowner.project.spy_priority_id%type := {priortyId}; " +
                                         "v_geom hpdowner.project.geom%type; " +
                                         $"v_external_id hpdowner.project.external_id%type := {k2processId}; " +
                                         $"v_assigned_user1 CONSTANT hpdowner.hydrodbusers.HYDRODBUSERS_ID%TYPE := {userId}; " +
                                         "v_assigned_users hpdowner.hpdnumber$table_type := hpdowner.hpdnumber$table_type(); " +
                                         "v_default_usage hpdowner.usage.usage_id%type := NULL; " +
                                         "BEGIN " +
                                         $"SELECT geom into v_geom FROM hpdowner.hpd_workspaces_vw where ws_name = '{workspace}'; " +
                                         "v_assigned_users.extend(1); " +
                                         "v_assigned_users(1) := hpdowner.hpdnumber$row_type(v_assigned_user1); " +
                                         "v_project_id := hpdowner.p_project_manager.addproject( " +
                                         "v_created_by, v_project_name, v_work_order, " +
                                         "v_start_date, v_process_time, " +
                                         "v_type_id, v_status_id, " +
                                         "v_priority_id, v_geom, " +
                                         "v_external_id, NULL, " +
                                         "v_assigned_users, v_default_usage); " +
                                         "END; ";

                    command.CommandText = projectCommand;
                    something = await command.ExecuteNonQueryAsync();
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw e;
                }
                return something;
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
