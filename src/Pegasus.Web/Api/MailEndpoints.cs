using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Pegasus.Contracts;
using Pegasus.Contracts.Mail;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Actors;
using Pegasus.Core.Cases;
using Pegasus.Core.Identity;
using Pegasus.Core.Intake;
using Pegasus.Core.Lifecycle;
using Pegasus.Core.Workflow;
using Pegasus.Web.Presentation;

namespace Pegasus.Web.Api;

/// <summary>Maps the retained mail workspace onto the existing Core use cases.</summary>
public static class MailEndpoints
{
    public static RouteGroupBuilder MapMailEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var mail = group.MapGroup("/mail")
            .RequireAuthorization()
            .AddEndpointFilter<VehicleAuthorizationEndpointFilter>();

        mail.MapGet(string.Empty, ListAsync)
            .WithName("ListMail")
            .WithSummary("List retained mail")
            .WithDescription("Returns retained mail newest first, its mailbox scopes and workspace freshness.")
            .Produces<MailPageResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json");

        mail.MapGet("/deleted", ListDeletedAsync)
            .WithName("SearchDeletedMail")
            .WithSummary("Search deleted mail")
            .WithDescription("Searches the exact approved mailbox Deleted Items folder without retaining or backfilling messages.")
            .Produces<DeletedMailPageResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json");

        mail.MapGet("/{messageId:guid}/preview", PreviewAsync)
            .WithName("PreviewMail")
            .WithSummary("Preview retained mail")
            .Produces<MailPreviewResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json");

        mail.MapGet("/{messageId:guid}", GetAsync)
            .WithName("GetMail")
            .WithSummary("Get retained mail detail")
            .Produces<MailDetailResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json");

        mail.MapPost("/{messageId:guid}/link-case/prepare", PrepareLinkCaseAsync)
            .WithName("PrepareMailCaseLink")
            .WithSummary("Prepare a retained-mail case link")
            .Produces<MailCasePreparationResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        mail.MapPost("/{messageId:guid}/unlink-case/prepare", PrepareUnlinkCaseAsync)
            .WithName("PrepareMailCaseUnlink")
            .WithSummary("Prepare a retained-mail case unlink")
            .Produces<MailCasePreparationResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        mail.MapPost("/{messageId:guid}/link-case", LinkCaseAsync)
            .WithName("LinkMailCase")
            .WithSummary("Link retained mail to a case")
            .Produces<MailCaseAssociationResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        mail.MapPost("/{messageId:guid}/unlink-case", UnlinkCaseAsync)
            .WithName("UnlinkMailCase")
            .WithSummary("Unlink retained mail from a case")
            .Produces<MailCaseAssociationResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        mail.MapPost("/{messageId:guid}/classification", CorrectClassificationAsync)
            .WithName("CorrectMailClassification")
            .WithSummary("Correct retained-mail classification")
            .Produces<MailClassificationResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        mail.MapPost("/{messageId:guid}/move-to-recommended-folder", MoveToRecommendedFolderAsync)
            .WithName("MoveMailToRecommendedFolder")
            .WithSummary("Move retained mail to its recommended folder")
            .Produces<MailFolderMoveResponse>()
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status503ServiceUnavailable, "application/problem+json");

        return group;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        ListRetainedMail listRetainedMail,
        GetRetainedMailFreshness getFreshness,
        string? mailbox,
        string? folder,
        int? page,
        int? pageSize,
        string? q,
        string? search,
        string? queue,
        string? destination,
        string? classification,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        var term = search ?? q;
        var scope = ParseScope(mailbox, folder, term, queue, destination, classification);
        var result = await listRetainedMail.ExecuteAsync(
            actor,
            scope,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken);
        var mailboxes = await listRetainedMail.ListMailboxesAsync(actor, cancellationToken);
        var freshness = await getFreshness.ExecuteAsync(actor, cancellationToken);
        var version = ProjectionVersion(new { result, mailboxes, freshness });
        var response = new MailPageResponse(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.HasUnretainedHistory,
            mailboxes.Select(Map).ToArray(),
            Map(freshness),
            version);
        return Conditional(httpContext, response, version);
    }

    private static Task<IResult> ListDeletedAsync(
        HttpContext httpContext,
        SearchDeletedMail searchDeletedMail,
        string? mailbox,
        int? page,
        int? pageSize,
        string? search,
        string? q,
        string? queue,
        CancellationToken cancellationToken) =>
        ListDeletedCoreAsync(
            httpContext,
            searchDeletedMail,
            VehicleAuthorizationEndpointFilter.GetActor(httpContext),
            mailbox,
            search ?? q,
            page,
            pageSize,
            queue,
            cancellationToken);

    private static async Task<IResult> ListDeletedCoreAsync(
        HttpContext httpContext,
        SearchDeletedMail searchDeletedMail,
        ActionActor actor,
        string? mailbox,
        string? search,
        int? page,
        int? pageSize,
        string? queue,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(queue))
        {
            throw new ArgumentException("Deleted Items search cannot be combined with a mail queue.", nameof(queue));
        }

        var result = await searchDeletedMail.ExecuteAsync(
            actor,
            mailbox,
            search ?? string.Empty,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken);
        var mailboxes = await searchDeletedMail.ListMailboxesAsync(actor, cancellationToken);
        var version = ProjectionVersion(new { result, mailboxes });
        var response = new DeletedMailPageResponse(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            result.IsTruncated,
            result.State.ToString(),
            mailboxes.Select(Map).ToArray(),
            version);
        return Conditional(httpContext, response, version);
    }

    private static async Task<IResult> PreviewAsync(
        Guid messageId,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        var detail = await getRetainedMail.ExecuteAsync(actor, messageId, cancellationToken);
        if (detail is null)
        {
            return NotFound(httpContext);
        }
        var summary = detail.Summary;
        var version = ProjectionVersion(detail);
        var response = new MailPreviewResponse(
            summary.Id,
            SenderLine(summary),
            SubjectLine(summary),
            summary.ReceivedAtUtc,
            $"{OperatorLabels.OfficeDate(summary.ReceivedAtUtc)} {OperatorLabels.OfficeClock(summary.ReceivedAtUtc)}",
            summary.BodyExcerpt ?? "No excerpt available",
            detail.Classification is { } classification
                ? DecisionLabel(classification.Current)
                : ClassificationLabel(detail.ClassificationOutcome),
            summary.CaseReference ?? "Not associated",
            detail.Attachments.Select(attachment => attachment.FileName).ToArray(),
            version);
        return Conditional(httpContext, response, version);
    }

    private static async Task<IResult> GetAsync(
        Guid messageId,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        string? search,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        var detail = await getRetainedMail.ExecuteAsync(actor, messageId, search, cancellationToken);
        if (detail is null)
        {
            return NotFound(httpContext);
        }
        var response = Map(detail);
        return Conditional(httpContext, response, response.Version);
    }

    private static async Task<IResult> PrepareLinkCaseAsync(
        Guid messageId,
        MailCasePreparationRequest request,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        IGetIntake getIntake,
        IGetCase getCase,
        IAcquireCaseEditLease acquireCaseEditLease,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireOperationKey(request.LeaseOperationKey, nameof(request.LeaseOperationKey));
        var association = await LoadAssociationAsync(messageId, actor, getRetainedMail, getIntake, cancellationToken);
        if (association is null)
        {
            return NotFound(httpContext);
        }
        RequireAssociationVersion(association.Receipt, request.ExpectedIntakeVersion);
        if (association.Receipt.CurrentCaseId is not null)
        {
            throw new IntakeVersionConflictException();
        }

        var selectedCase = await getCase.ExecuteAsync(new(request.CaseId, actor), cancellationToken);
        if (selectedCase is null)
        {
            return NotFound(httpContext);
        }
        RequireEligibleCase(selectedCase, request.ExpectedCaseVersion);
        var lease = await acquireCaseEditLease.ExecuteAsync(
            new(request.CaseId, request.ExpectedCaseVersion, actor, request.LeaseOperationKey),
            cancellationToken);
        return TypedResults.Ok(new MailCasePreparationResponse(
            messageId,
            association.Receipt.Id,
            "link",
            request.CaseId,
            request.ExpectedIntakeVersion,
            request.ExpectedCaseVersion,
            lease.Token,
            lease.ExpiresAtUtc));
    }

    private static async Task<IResult> PrepareUnlinkCaseAsync(
        Guid messageId,
        MailCasePreparationRequest request,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        IGetIntake getIntake,
        IGetCase getCase,
        IAcquireCaseEditLease acquireCaseEditLease,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireOperationKey(request.LeaseOperationKey, nameof(request.LeaseOperationKey));
        var association = await LoadAssociationAsync(messageId, actor, getRetainedMail, getIntake, cancellationToken);
        if (association is null)
        {
            return NotFound(httpContext);
        }
        RequireAssociationVersion(association.Receipt, request.ExpectedIntakeVersion);
        if (association.Receipt.CurrentCaseId != request.CaseId)
        {
            throw new IntakeVersionConflictException();
        }

        var currentCase = await getCase.ExecuteAsync(new(request.CaseId, actor), cancellationToken);
        if (currentCase is null)
        {
            return NotFound(httpContext);
        }
        if (currentCase.Workflow.Version != request.ExpectedCaseVersion)
        {
            throw new IntakeVersionConflictException();
        }

        var lease = await acquireCaseEditLease.ExecuteAsync(
            new(request.CaseId, request.ExpectedCaseVersion, actor, request.LeaseOperationKey),
            cancellationToken);
        return TypedResults.Ok(new MailCasePreparationResponse(
            messageId,
            association.Receipt.Id,
            "unlink",
            request.CaseId,
            request.ExpectedIntakeVersion,
            request.ExpectedCaseVersion,
            lease.Token,
            lease.ExpiresAtUtc,
            association.Receipt.UnlinkCancelsCase
                ? $"Unlinking this email cancels case {currentCase.Summary.Reference}"
                : null));
    }

    private static async Task<IResult> LinkCaseAsync(
        Guid messageId,
        MailCaseAssociationRequest request,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        IGetIntake getIntake,
        IGetCase getCase,
        ILinkIntake linkIntake,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireConfirmation(request.EditLeaseToken, request.OperationKey, request.Reason);
        var association = await LoadAssociationAsync(messageId, actor, getRetainedMail, getIntake, cancellationToken);
        if (association is null)
        {
            return NotFound(httpContext);
        }
        var isReplay = IsAssociationReplay(association.Receipt, request.OperationKey);
        if (!isReplay)
        {
            RequireAssociationVersion(association.Receipt, request.ExpectedIntakeVersion);
            if (association.Receipt.CurrentCaseId is not null)
            {
                throw new IntakeVersionConflictException();
            }
            var targetCase = await getCase.ExecuteAsync(new(request.CaseId, actor), cancellationToken);
            if (targetCase is null)
            {
                return NotFound(httpContext);
            }
            RequireEligibleCase(targetCase, request.ExpectedCaseVersion);
        }
        var command = new LinkIntakeRequest(
            association.Receipt.Id,
            request.CaseId,
            request.ExpectedIntakeVersion,
            request.ExpectedCaseVersion,
            request.EditLeaseToken,
            actor,
            request.OperationKey,
            request.Reason);
        if (isReplay)
        {
            await linkIntake.ExecuteAsync(command, cancellationToken);
        }
        else
        {
            await ExecuteWithAuditAsync(
                action: () => linkIntake.ExecuteAsync(command, cancellationToken),
                actionHistory,
                timeProvider,
                actor,
                messageId,
                "mail_case_link",
                request.OperationKey,
                request.Reason,
                cancellationToken);
        }
        var updated = await getIntake.ExecuteAsync(new(association.Receipt.Id, actor), cancellationToken)
            ?? throw new InvalidDataException("The linked intake receipt could not be reloaded.");
        return TypedResults.Ok(new MailCaseAssociationResponse(
            messageId,
            updated.Id,
            "link",
            request.CaseId,
            updated.Version));
    }

    private static async Task<IResult> UnlinkCaseAsync(
        Guid messageId,
        MailCaseAssociationRequest request,
        HttpContext httpContext,
        GetRetainedMail getRetainedMail,
        IGetIntake getIntake,
        IGetCase getCase,
        IReverseIntakeLink reverseIntakeLink,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireConfirmation(request.EditLeaseToken, request.OperationKey, request.Reason);
        var association = await LoadAssociationAsync(messageId, actor, getRetainedMail, getIntake, cancellationToken);
        if (association is null)
        {
            return NotFound(httpContext);
        }
        var isReplay = IsAssociationReplay(association.Receipt, request.OperationKey);
        var consequence = UnlinkConsequence(association.Receipt, request.CaseId);
        if (!isReplay)
        {
            RequireAssociationVersion(association.Receipt, request.ExpectedIntakeVersion);
            if (association.Receipt.CurrentCaseId != request.CaseId)
            {
                throw new IntakeVersionConflictException();
            }
            var currentCase = await getCase.ExecuteAsync(new(request.CaseId, actor), cancellationToken);
            if (currentCase is null)
            {
                return NotFound(httpContext);
            }
            if (currentCase.Workflow.Version != request.ExpectedCaseVersion)
            {
                throw new IntakeVersionConflictException();
            }
        }
        var command = new ReverseIntakeLinkRequest(
            association.Receipt.Id,
            request.CaseId,
            request.ExpectedIntakeVersion,
            request.ExpectedCaseVersion,
            request.EditLeaseToken,
            actor,
            request.OperationKey,
            request.Reason);
        if (isReplay)
        {
            await reverseIntakeLink.ExecuteAsync(command, cancellationToken);
        }
        else
        {
            await ExecuteWithAuditAsync(
                action: () => reverseIntakeLink.ExecuteAsync(command, cancellationToken),
                actionHistory,
                timeProvider,
                actor,
                messageId,
                "mail_case_unlink",
                request.OperationKey,
                request.Reason,
                cancellationToken);
        }
        var updated = await getIntake.ExecuteAsync(new(association.Receipt.Id, actor), cancellationToken)
            ?? throw new InvalidDataException("The unlinked intake receipt could not be reloaded.");
        return TypedResults.Ok(new MailCaseAssociationResponse(
            messageId,
            updated.Id,
            "unlink",
            request.CaseId,
            updated.Version,
            consequence));
    }

    private static async Task<IResult> CorrectClassificationAsync(
        Guid messageId,
        MailClassificationCorrectionRequest request,
        HttpContext httpContext,
        CorrectRetainedMailClassification correctClassification,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireOperationKey(request.OperationKey, nameof(request.OperationKey));
        if (!MailClassificationSelection.TryParse(
                request.ClassificationKey?.Trim(),
                request.OtherName,
                request.OtherReasoning,
                out var category)
            || category is null)
        {
            throw new ArgumentException("The classification key is not a canonical correction option.", nameof(request));
        }

        var dossier = await ExecuteWithAuditAsync(
            action: async () => await correctClassification.ExecuteAsync(
                actor,
                new(
                    messageId,
                    request.ExpectedClassificationVersion,
                    category,
                    request.Reason),
                cancellationToken)
                ?? throw new KeyNotFoundException("The retained message was not found or has no classification decision."),
            actionHistory,
            timeProvider,
            actor,
            messageId,
            "mail_classification_correction",
            request.OperationKey,
            request.Reason,
            cancellationToken);
        return TypedResults.Ok(Map(dossier));
    }

    private static async Task<IResult> MoveToRecommendedFolderAsync(
        Guid messageId,
        MailMoveRequest request,
        HttpContext httpContext,
        MoveRetainedMailFolder moveRetainedMailFolder,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = VehicleAuthorizationEndpointFilter.GetActor(httpContext);
        RequireOperationKey(request.OperationKey, nameof(request.OperationKey));
        var result = await ExecuteWithAuditAsync(
            action: async () => await moveRetainedMailFolder.ExecuteAsync(
                actor,
                new(
                    messageId,
                    request.ExpectedClassificationVersion,
                    request.ExpectedRecommendationPolicyKey,
                    request.ExpectedRecommendationPolicyVersion,
                    request.ExpectedMailboxVersion,
                    request.OperationKey,
                    request.Reason),
                cancellationToken)
                ?? throw new KeyNotFoundException("The retained message was not found or has no movable folder recommendation."),
            actionHistory,
            timeProvider,
            actor,
            messageId,
            "mail_folder_move",
            request.OperationKey,
            request.Reason,
            cancellationToken);
        return TypedResults.Ok(Map(result));
    }

    private static async Task<AssociationContext?> LoadAssociationAsync(
        Guid messageId,
        ActionActor actor,
        GetRetainedMail getRetainedMail,
        IGetIntake getIntake,
        CancellationToken cancellationToken)
    {
        var detail = await getRetainedMail.ExecuteAsync(actor, messageId, cancellationToken);
        if (detail?.Summary.IntakeReceiptId is not { } receiptId)
        {
            return null;
        }

        var receipt = await getIntake.ExecuteAsync(new(receiptId, actor), cancellationToken);
        return receipt is null ? null : new(receipt);
    }

    private static void RequireAssociationVersion(IntakeReceipt receipt, long expectedVersion)
    {
        if (receipt.Version != expectedVersion)
        {
            throw new IntakeVersionConflictException();
        }
    }

    private static void RequireEligibleCase(CaseDetails selectedCase, long expectedVersion)
    {
        if (selectedCase.Workflow.Version != expectedVersion
            || selectedCase.Workflow.Archive is not null
            || CaseLifecycleRules.IsTerminal(selectedCase.Workflow.State))
        {
            throw new IntakeVersionConflictException();
        }
    }

    private static bool IsAssociationReplay(IntakeReceipt receipt, string operationKey) =>
        string.Equals(
            receipt.ManualAssociationOperationKey,
            operationKey.Trim(),
            StringComparison.Ordinal);

    private static string? UnlinkConsequence(IntakeReceipt receipt, Guid caseId) =>
        receipt.AcceptedCaseId == caseId && receipt.CurrentCaseReference is { } reference
            ? $"Unlinking this email cancels case {reference}"
            : null;

    private static void RequireConfirmation(
        string? editLeaseToken,
        string? operationKey,
        string? reason)
    {
        RequireText(editLeaseToken, nameof(editLeaseToken), 200);
        RequireOperationKey(operationKey, nameof(operationKey));
        RequireText(reason, nameof(reason), 500);
    }

    private static void RequireOperationKey(string? value, string parameterName)
    {
        RequireText(value, parameterName, 100);
    }

    private static void RequireText(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} is required and must be {maximumLength} characters or fewer.",
                parameterName);
        }
    }

    private static MailWorkspaceScope ParseScope(
        string? mailbox,
        string? folder,
        string? search,
        string? queue,
        string? destinationValue,
        string? classificationValue)
    {
        var folderValue = folder?.Trim();
        var folderScope = folderValue switch
        {
            null or "" or "inbox" => MailFolderScope.Inbox,
            "sent" => MailFolderScope.Sent,
            _ => throw new ArgumentException("The folder scope must be inbox or sent.", nameof(folder))
        };
        var (queueDestination, queueClassification) = ParseQueue(queue);
        var explicitDestination = ParseDestination(destinationValue);
        var explicitClassification = ParseClassification(classificationValue);
        if ((queueDestination is not null || explicitDestination is not null)
            && (queueClassification is not null || explicitClassification is not null))
        {
            throw new ArgumentException(
                "Choose either an operational destination or one detailed classification.",
                nameof(queue));
        }
        if (queueDestination is not null && explicitDestination is not null
            || queueClassification is not null && explicitClassification is not null)
        {
            throw new ArgumentException(
                "The mail view was supplied more than once.",
                nameof(queue));
        }
        return new(
            string.IsNullOrWhiteSpace(mailbox) ? null : mailbox.Trim(),
            folderScope,
            search,
            queueDestination ?? explicitDestination,
            queueClassification ?? explicitClassification);
    }

    private static (MailOperationalDestination?, MailCategory?) ParseQueue(string? queue)
    {
        if (string.IsNullOrWhiteSpace(queue))
        {
            return (null, null);
        }

        var value = queue.Trim();
        var destination = value switch
        {
            "receiving-work" => MailOperationalDestination.ReceivingWork,
            "queries" => MailOperationalDestination.Queries,
            "other" => MailOperationalDestination.Other,
            "unidentified" => MailOperationalDestination.Unidentified,
            "triage" => MailOperationalDestination.Triage,
            _ => (MailOperationalDestination?)null
        };
        if (destination is not null)
        {
            return (destination, null);
        }

        const string prefix = "classification:";
        if (value.StartsWith(prefix, StringComparison.Ordinal)
            && MailClassificationSelection.TryParse(
                value[prefix.Length..],
                null,
                null,
                out var category)
            && category is not null
            && MailOperationalDestinationPolicy.Map(category).Destination
                == MailOperationalDestination.DetailedClassification)
        {
            return (null, category);
        }

        throw new ArgumentException("The mail queue is not a canonical operational or classification view.", nameof(queue));
    }

    private static MailOperationalDestination? ParseDestination(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim() switch
        {
            "receiving-work" => MailOperationalDestination.ReceivingWork,
            "queries" => MailOperationalDestination.Queries,
            "other" => MailOperationalDestination.Other,
            "unidentified" => MailOperationalDestination.Unidentified,
            "triage" => MailOperationalDestination.Triage,
            _ => throw new ArgumentException(
                "The mail destination is not a canonical operational view.",
                nameof(value))
        };
    }

    private static MailCategory? ParseClassification(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim();
        const string prefix = "classification:";
        if (key.StartsWith(prefix, StringComparison.Ordinal))
        {
            key = key[prefix.Length..];
        }
        if (!MailClassificationSelection.TryParse(key, null, null, out var category)
            || category is null
            || MailOperationalDestinationPolicy.Map(category).Destination
                != MailOperationalDestination.DetailedClassification)
        {
            throw new ArgumentException(
                "The mail classification is not a canonical detailed view.",
                nameof(value));
        }

        return category;
    }

    private static string SenderLine(RetainedMailSummary item) =>
        item.EffectiveSenderAddress
        ?? item.SenderDisplayName
        ?? item.SenderAddress
        ?? "Sender not recorded";

    private static string SubjectLine(RetainedMailSummary item) =>
        string.IsNullOrWhiteSpace(item.Subject) ? "No subject" : item.Subject;

    private static string ClassificationLabel(MailClassificationOutcome? outcome) => outcome switch
    {
        MailClassificationOutcome.Classified => "Classified",
        MailClassificationOutcome.Ambiguous => "Ambiguous",
        MailClassificationOutcome.Unclassified => "Unclassified",
        _ => "Not yet processed"
    };

    private static string DecisionLabel(MailClassificationResult result) => result.Category is { } category
        ? OperatorLabels.MailClassification(category)
        : ClassificationLabel(result.Outcome);

    private static MailSummaryResponse Map(RetainedMailSummary summary) => new(
        summary.Id,
        summary.MailboxId,
        summary.MailboxAddress,
        summary.MailboxIsPolled,
        summary.SenderAddress,
        summary.SenderDisplayName,
        summary.EffectiveSenderAddress,
        summary.Subject,
        summary.BodyExcerpt,
        summary.ReceivedAtUtc,
        summary.IsRead,
        summary.AttachmentCount,
        summary.ProcessingOutcome?.ToString(),
        summary.IntakeReceiptId,
        summary.CaseId,
        summary.CaseReference,
        summary.AllocationState?.ToString(),
        summary.Matches.Select(Map).ToArray(),
        summary.CurrentFolderType?.ToString(),
        summary.Classification is { } classification ? Map(classification) : null,
        summary.OperationalDestination?.Destination.ToString(),
        summary.IntakeVersion,
        summary.CaseVersion);

    private static MailDetailResponse Map(RetainedMailDetail detail)
    {
        var version = ProjectionVersion(detail);
        return new(
            Map(detail.Summary),
            detail.ToAddresses,
            detail.CcAddresses,
            detail.BodyPlainText,
            detail.Attachments.Select(Map).ToArray(),
            detail.Thread.Select(item => new MailThreadEntryResponse(
                item.Id,
                item.SenderDisplayName,
                item.SenderAddress,
                item.Subject,
                item.ReceivedAtUtc)).ToArray(),
            detail.Folder.ToString(),
            detail.ClassificationOutcome?.ToString(),
            detail.RouteDisposition?.ToString(),
            detail.Classification is { } classification ? Map(classification) : null,
            detail.FolderRecommendation is { } recommendation ? Map(recommendation) : null,
            detail.LatestFolderMove is { } move ? Map(move) : null,
            detail.SuggestedMove is { } suggested ? new(suggested.FolderType.ToString(), suggested.Reason) : null,
            version);
    }

    private static MailClassificationResponse Map(MailClassificationDossier dossier) => new(
        dossier.Version,
        Map(dossier.Current),
        dossier.CurrentActorDisplayName,
        dossier.CurrentDecidedAtUtc,
        MailOperationalDestinationPolicy.Map(dossier.Current).Destination.ToString(),
        dossier.History.Select(entry => new MailClassificationHistoryResponse(
            entry.Version,
            Map(entry.Before),
            Map(entry.After),
            entry.ActorDisplayName,
            entry.Reason,
            entry.CorrectedAtUtc)).ToArray(),
        MailClassificationSelection.Options
            .Select(option => new MailClassificationOptionResponse(option.Value, option.Label))
            .ToArray());

    private static MailClassificationResultResponse Map(MailClassificationResult result) => new(
        result.Outcome.ToString(),
        result.Category is { } category ? Map(category) : null,
        result.AmbiguousCandidates,
        result.Predicates.Select(predicate => new MailPredicateResponse(
            predicate.Key,
            predicate.Matched,
            predicate.Detail)).ToArray(),
        result.Reason,
        result.PolicyKey,
        result.PolicyVersion,
        result.CaseType?.ToString(),
        result.StandaloneAuditReport is { } report
            ? new MailStandaloneAuditResponse(report.AssetSourceLabel, report.Assessment.ToString())
            : null);

    private static MailCategoryResponse Map(MailCategory category) => new(
        category.Direction.ToString(),
        category.Name,
        category.ReceivedFamily?.ToString(),
        category.SentFamily?.ToString(),
        category.Subtype,
        category.IsReplyContext,
        category.IsOther,
        category.OtherName,
        category.OtherReasoning);

    private static MailFolderRecommendationResponse Map(RetainedMailFolderRecommendation recommendation) => new(
        recommendation.FolderType?.ToString(),
        recommendation.PolicyKey,
        recommendation.PolicyVersion,
        recommendation.Reason,
        recommendation.MailboxVersion,
        recommendation.CanMove);

    private static MailFolderMoveResponse Map(RetainedMailFolderMoveResult result) => new(
        result.Outcome.ToString(),
        result.FolderType.ToString(),
        result.Reason,
        result.RecordedAtUtc,
        result.IsReplay,
        result.OperationKey,
        result.FailureReason,
        result.ExpectedClassificationVersion,
        result.ExpectedRecommendationPolicyKey,
        result.ExpectedRecommendationPolicyVersion,
        result.ExpectedMailboxVersion,
        result.Outcome switch
        {
            RetainedMailFolderMoveOutcome.Succeeded =>
                "Message moved to the recommended Outlook folder.",
            RetainedMailFolderMoveOutcome.Failed =>
                "The message was not moved. You can retry with a new confirmation.",
            RetainedMailFolderMoveOutcome.Uncertain =>
                "The move result is uncertain. Retry this same confirmation to check its current location.",
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        });

    private static MailSearchMatchResponse Map(RetainedMailSearchMatch match) => new(
        match.Kind.ToString(),
        match.AttachmentFileName,
        match.AttachmentOrdinal);

    private static MailAttachmentResponse Map(RetainedMailAttachment attachment) => new(
        attachment.FileName,
        attachment.MediaType,
        attachment.ContentLength,
        attachment.IsSearchable);

    private static DeletedMailItemResponse Map(DeletedMailSearchItem item) => new(
        item.MailboxId,
        item.MailboxAddress,
        item.ImmutableMessageId,
        item.SenderAddress,
        item.SenderDisplayName,
        item.Subject,
        item.BodyPlainText,
        item.ReceivedAtUtc,
        item.IsRead,
        item.Attachments.Select(Map).ToArray(),
        item.Matches.Select(Map).ToArray());

    private static MailboxResponse Map(RetainedMailMailbox mailbox) => new(
        mailbox.MailboxId,
        mailbox.MailboxAddress,
        mailbox.IsPolled);

    private static MailFreshnessResponse Map(MailFreshness freshness) => new(
        freshness.State.ToString(),
        freshness.LastSuccessfulUpdateAtUtc);

    private static string ProjectionVersion<T>(T value)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, PegasusJson.Options));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static IResult Conditional<T>(HttpContext httpContext, T response, string version)
    {
        var etag = $"W/\"{version}\"";
        httpContext.Response.Headers.ETag = etag;
        if (httpContext.Request.Headers.IfNoneMatch
            .ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(response);
    }

    private static IResult NotFound(HttpContext httpContext) => DesktopGatewayProblems.NotFound(httpContext);

    private static async Task ExecuteWithAuditAsync(
        Func<Task> action,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        ActionActor actor,
        Guid messageId,
        string eventKind,
        string operationKey,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await action();
            await AppendAuditAsync(
                actionHistory,
                timeProvider,
                actor,
                messageId,
                eventKind,
                operationKey,
                "Succeeded",
                reason: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await AppendAuditAsync(
                actionHistory,
                timeProvider,
                actor,
                messageId,
                eventKind,
                operationKey,
                "Failed",
                $"{exception.GetType().Name}: {exception.Message}",
                cancellationToken);
            throw;
        }
    }

    private static async Task<TResult> ExecuteWithAuditAsync<TResult>(
        Func<Task<TResult>> action,
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        ActionActor actor,
        Guid messageId,
        string eventKind,
        string operationKey,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await action();
            await AppendAuditAsync(
                actionHistory,
                timeProvider,
                actor,
                messageId,
                eventKind,
                operationKey,
                "Succeeded",
                reason: null,
                cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await AppendAuditAsync(
                actionHistory,
                timeProvider,
                actor,
                messageId,
                eventKind,
                operationKey,
                "Failed",
                $"{exception.GetType().Name}: {exception.Message}",
                cancellationToken);
            throw;
        }
    }

    private static Task AppendAuditAsync(
        IActionHistoryWriter actionHistory,
        TimeProvider timeProvider,
        ActionActor actor,
        Guid messageId,
        string eventKind,
        string operationKey,
        string outcome,
        string? reason,
        CancellationToken cancellationToken) =>
        actionHistory.AppendAsync(
            new(
                Guid.NewGuid(),
                "mail_api",
                messageId.ToString("D"),
                eventKind,
                actor,
                timeProvider.GetUtcNow(),
                outcome,
                operationKey.Trim(),
                reason),
            cancellationToken);

    private sealed record AssociationContext(IntakeReceipt Receipt);
}
