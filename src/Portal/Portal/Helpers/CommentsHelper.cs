using System;
using System.Threading.Tasks;
using Portal.Auth;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IPortalUserDbService _portalUserDbService;

        public CommentsHelper(WorkflowDbContext dbContext, IPortalUserDbService portalUserDbService)
        {
            _dbContext = dbContext;
            _portalUserDbService = portalUserDbService;
        }

        public async Task AddComment(string comment, int processId, int workflowInstanceId, string userPrincipalName)
        {
            await _dbContext.Comments.AddAsync(new Comment
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                AdUser = await _portalUserDbService.GetAdUserAsync(userPrincipalName),
                Text = comment
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}