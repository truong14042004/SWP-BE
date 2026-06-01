namespace SWP_BE.Services;

public interface IAutoEvolveAiService
{
    Task GenerateProposalsAsync(Guid careerRoleId, CancellationToken cancellationToken);
}
