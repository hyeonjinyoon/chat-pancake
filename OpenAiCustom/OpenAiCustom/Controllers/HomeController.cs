using System.Diagnostics;
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
        Console.WriteLine($"Received instructions: {instructions}");

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
            questionText = $"Question: {question} , Instructions : {instructions}";

        var answer = await OpenAiManager.GetChat(apiKey, model.modelId, questionText);

        var timeComplete = DateTime.Now;

        answerHtml = answerHtml.Replace("%%%Second", $"{(timeComplete - timeBegin).TotalSeconds:N0}");

        var data = new
        {
            html = answerHtml,
            content = answer,
            answerId = $"answer-{answerId}",
        };

        return Json(data);
    }
}
