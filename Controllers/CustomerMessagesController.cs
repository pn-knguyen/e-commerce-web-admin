using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.CustomerMessages;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("CustomerMessages", Permissions.View)]
public sealed class CustomerMessagesController(
    ICustomerMessageAdminService customerMessageService,
    UserManager<Staff> userManager) : Controller
{
    public async Task<IActionResult> Index(
        [FromQuery] CustomerMessageIndexQuery query,
        long? id,
        [FromQuery] bool listOnly = false,
        CancellationToken ct = default)
    {
        return View(await BuildWorkspaceAsync(query, id, listOnly, ct));
    }

    public IActionResult Details(long id)
    {
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Messages(
        long id,
        long? beforeId,
        int take = 50,
        CancellationToken ct = default)
    {
        var page = await customerMessageService.GetMessagesAsync(id, beforeId, take, ct);
        if (page is null)
        {
            return NotFound();
        }

        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        return Ok(new
        {
            messages = page.Messages.Select(message => new
            {
                message.Id,
                conversationId = id,
                sender = message.Sender.ToString(),
                senderLabel = CustomerMessageDisplay.GetSenderLabel(message.Sender),
                senderClass = CustomerMessageDisplay.GetSenderClass(message.Sender),
                message.SenderName,
                message.Body,
                message.IsReadByAdmin,
                message.AiProvider,
                message.AiModel,
                message.AiPrompt,
                message.AiResponseId,
                message.AiMetadataJson,
                createdAtIso = message.CreatedAt.ToString("O"),
                createdAtText = TimeZoneInfo.ConvertTimeFromUtc(
                    message.CreatedAt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(message.CreatedAt, DateTimeKind.Utc) : message.CreatedAt.ToUniversalTime(), 
                    vnZone).ToString("dd/MM/yyyy HH:mm", vi),
            }),
            page.HasMore,
            page.NextBeforeId,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("CustomerMessages", Permissions.Edit)]
    public async Task<IActionResult> Reply(
        long id,
        CustomerMessageReplyViewModel form,
        CancellationToken ct = default)
    {
        if (id != form.ConversationId)
        {
            return BadRequest();
        }

        var staffId = await GetCurrentStaffIdAsync();
        if (!staffId.HasValue)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var viewModel = await BuildWorkspaceAsync(new CustomerMessageIndexQuery(), id, false, ct);
            if (viewModel.Conversation is null)
            {
                return NotFound();
            }

            viewModel.Conversation.ReplyForm = form;
            return View("Index", viewModel);
        }

        var result = await customerMessageService.SendStaffReplyAsync(id, staffId.Value, form, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            var viewModel = await BuildWorkspaceAsync(new CustomerMessageIndexQuery(), id, false, ct);
            if (viewModel.Conversation is null)
            {
                return NotFound();
            }

            viewModel.Conversation.ReplyForm = form;
            return View("Index", viewModel);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("CustomerMessages", Permissions.Edit)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        CustomerConversationStatusUpdateViewModel form,
        CancellationToken ct = default)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var staffId = await GetCurrentStaffIdAsync();
        if (!staffId.HasValue)
        {
            return Unauthorized();
        }

        var result = await customerMessageService.UpdateStatusAsync(id, staffId.Value, form, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            var viewModel = await BuildWorkspaceAsync(new CustomerMessageIndexQuery(), id, false, ct);
            if (viewModel.Conversation is null)
            {
                return NotFound();
            }

            return View("Index", viewModel);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { id });
    }

    private async Task<CustomerMessageWorkspaceViewModel> BuildWorkspaceAsync(
        CustomerMessageIndexQuery query,
        long? selectedId,
        bool listOnly,
        CancellationToken ct)
    {
        var index = await customerMessageService.GetIndexAsync(query, ct);
        if (listOnly)
        {
            return new CustomerMessageWorkspaceViewModel
            {
                Index = index,
                IsListOnly = true,
            };
        }

        selectedId ??= index.Conversations.FirstOrDefault()?.Id;
        if (!selectedId.HasValue)
        {
            return new CustomerMessageWorkspaceViewModel { Index = index };
        }

        var selectedRow = index.Conversations.FirstOrDefault(item => item.Id == selectedId.Value);
        var unreadBeforeOpen = selectedRow?.UnreadCustomerMessageCount ?? 0;
        var conversation = await customerMessageService.GetDetailsAsync(selectedId.Value, ct);

        if (selectedRow is not null && unreadBeforeOpen > 0)
        {
            selectedRow.UnreadCustomerMessageCount = 0;
            index.UnreadCustomerMessageCount = Math.Max(0, index.UnreadCustomerMessageCount - unreadBeforeOpen);
            
            if (query.Unread)
            {
                index.Conversations = index.Conversations.Where(c => c.Id != selectedId.Value).ToList();
            }
        }

        return new CustomerMessageWorkspaceViewModel
        {
            Index = index,
            Conversation = conversation,
        };
    }

    private async Task<long?> GetCurrentStaffIdAsync()
    {
        var staff = await userManager.GetUserAsync(User);
        return staff?.Id;
    }

    private void AddErrors(IEnumerable<CustomerMessageValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
