using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using OpenAiCustom.Managers;
using OpenAiCustom.Models;

namespace OpenAiCustom.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [HttpPost]
    public async Task<JsonResult> GetChatList([FromBody] ModelDefault model)
    {
        var partitionHash = ComputeSha256Hash(model.data);

        var conditions = new List<ScanCondition>
        {
            new("partition", ScanOperator.Equal, partitionHash),
        };

        var list = await AwsManager.DbContext.ScanAsync<PancakeChat>(conditions).GetRemainingAsync();

        var template = $"""
                            <div
                                class="click-color unselectable"
                               onclick="LoadChat('sortId', 'chatId')"
                               style="width:95%; cursor: pointer; background-color: transparent;  border-radius: 10px; padding: 4px 8px;">
                               chatName
                            </div>
                        """;

        var data = new
        {
            html = template,
            content = list.Select<PancakeChat, object>(chat => new
            {
                id = chat.id,
                name = chat.questionCondensed,
                chatId = chat.chatId
            }).ToList(),
        };

        return Json(data);
    }

    [HttpPost]
    public async Task<JsonResult> GetChat([FromBody] ModelDefaultWithApiKey model)
    {
        var chatId = model.data;
        var apiKey = model.apiKey;
        var partitionHash = ComputeSha256Hash(apiKey);

        var chat = await AwsManager.DbContext.LoadAsync<PancakeChat>(partitionHash, chatId);

        var chatAnswerId = $"answer-{chat.answerId}";

        //todo: 아래 ReceiveText랑 공통 부분 묶기
        var answerHtml = $"""
                          <div style="color: #707070;">
                          {chat.creationElapsedTime:N0}초 동안 조리
                          </div>
                          <div
                              id="{chatAnswerId}"
                              style="margin-top: 8px;
                              margin-right: auto; 
                              width: fit-content; 
                              background-color: #303030; 
                              color: #ccc; padding: 9px; 
                              border-radius: 20px; 
                              max-width: 600px;
                              white-space: pre-wrap;
                              overflow: auto;
                              align-items: center;"></div>
                          
                              <div style='width: 20px; height: 20px; margin-right: auto; color: #919191; cursor: pointer;' onclick=copyToClipboard('{chatAnswerId}')>
                              <svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-copy' viewBox='0 0 16 16'><path fill-rule='evenodd' d='M4 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1zM2 5a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-1h1v1a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h1v1z'/></svg>
                              </div>
                          """;

        var data = new
        {
            html = answerHtml,
            content = chat.content,
            answerId = chatAnswerId,
            question = chat.question,
        };

        return Json(data);
    }

    [HttpPost]
    public async Task<JsonResult> ReceiveText([FromBody] ContentTextDataModel model)
    {
        var answerId = Guid.NewGuid().ToString();

        var answerHtml = $"""
                          <div style="color: #707070;">
                          %%%Second초 동안 조리
                          </div>
                          <div
                              id="answer-{answerId}"
                              style="margin-top: 8px;
                              margin-right: auto; 
                              width: fit-content; 
                              background-color: #303030; 
                              color: #ccc; padding: 9px; 
                              border-radius: 20px; 
                              max-width: 600px;
                              white-space: pre-wrap;
                              overflow: auto;
                              align-items: center;"></div>
                          
                              <div style='width: 20px; height: 20px; margin-right: auto; color: #919191; cursor: pointer;' onclick=copyToClipboard('answer-{answerId}')>
                              <svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-copy' viewBox='0 0 16 16'><path fill-rule='evenodd' d='M4 2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zm2-1a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1V2a1 1 0 0 0-1-1zM2 5a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-1h1v1a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h1v1z'/></svg>
                              </div>
                          """;

        if (string.IsNullOrEmpty(model.data))
        {
            answerHtml = answerHtml.Replace("%%%Second", "0");
            return Json(new
            {
                html = answerHtml,
                content = "No data provided",
                answerId = $"answer-{answerId}",
            });
        }

        var question = model.data;
        var instructions = model.instructions;
        var apiKey = model.apiKey;

        Console.WriteLine($"Received question: {question}");

        var noSave = question.Contains("!nosave");

        if (noSave)
            question = question.Replace("!nosave", "");

        var timeBegin = DateTime.Now;

        if (string.IsNullOrEmpty(apiKey))
        {
            answerHtml = answerHtml.Replace("%%%Second", "0");
            return Json(new
            {
                html = answerHtml,
                content = "API 키 세팅이 필요합니다",
                answerId = $"answer-{answerId}",
            });
        }

        var questionText = "";

        if (string.IsNullOrEmpty(instructions))
            questionText = question;
        else
            questionText = $"Please read the question and instructions below and answer accordingly.\nQuestion:{{{question}}},Instructions:{{{instructions}}}";
        
        var answer = await OpenAiManager.GetChat(apiKey, model.modelId, questionText);

        var timeComplete = DateTime.Now;

        var creationElapsedTime = (timeComplete - timeBegin).TotalSeconds;

        answerHtml = answerHtml.Replace("%%%Second", $"{creationElapsedTime:N0}");

        if (noSave)
            answer = "(This conversation is not saved.)\n" + answer;

        var sortKey = $"{DateTime.UtcNow.Ticks}-{answerId}";
        var questionCondensed = question.Length < 9 ? question : question[..9] + "..";
        var chatId = model.chatId;

        var listHtml = $"""
                            <div
                                class="click-color unselectable"
                               onclick="LoadChat('{sortKey}', '{chatId}')"
                               style="width:95%; cursor: pointer; background-color: transparent;  border-radius: 10px; padding: 4px 8px;">
                               {questionCondensed}
                            </div>
                        """;

        var data = new
        {
            html = answerHtml,
            content = answer,
            answerId = $"answer-{answerId}",
            listHtml = listHtml
        };

        if (noSave)
            return Json(data);

        var partitionHash = ComputeSha256Hash(apiKey);

        var chatData = new PancakeChat
        {
            dateTime = DateTime.UtcNow,
            partition = partitionHash,
            id = sortKey,
            creationElapsedTime = creationElapsedTime,
            question = question,
            questionCondensed = questionCondensed,
            instructions = instructions,
            content = answer,
            answerId = answerId,
            modelId = model.modelId,
            chatId = chatId
        };

        await AwsManager.DbContext.SaveAsync(chatData);

        return Json(data);
    }

    private string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
