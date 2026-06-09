using System;
using System.Threading;
using System.Threading.Tasks;
using SWP_BE.Controllers;

namespace SWP_BE.Services;

public interface IRoadmapMaterializer
{
    Task<MaterializeResult> MaterializeRoadmapAsync(
        Guid userId,
        AiRoadmapDto roadmapDto,
        CancellationToken cancellationToken);
}

public sealed record MaterializeResult(
    Guid RoadmapId,
    string Title,
    int NodeCount,
    bool IsExisting);
