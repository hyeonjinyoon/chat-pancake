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
        if (string.IsNullOrEmpty(model.Data))
            return Json(new
            {
                error = "No data provided"
            });

        var question = model.Data;

        Console.WriteLine($"Received question: {question}");

//         var context = """
//                       If you think that splitting the question into multiple parts and re-asking it might yield better results,
//                       then please separate the question into several questions and answer them.
//                       In that case, start your answer with "$$$separate",
//                       followed by a JSON structure containing the separated questions.
//                       If you feel it's unnecessary, just answer in a normal way.
//                       
//                       Example:
//                       
//                       Question: "Please make three example sentences for each of those words, 'assignment, bend over, commonplace'",
//                       Answer: "$$$separate{"questions" : ["Make three example sentences using the word 'assignment'","Make three example sentences using the word 'bend over'","Make three example sentences using the word 'commonplace'"]}"
//                       
//                       Question: "test",
//                       Answer: "Hello there! How can I assist you today?"
//                       """;
//         
//         var questionWithContext = $"""
//                                    "You are a program that uses the ChatGPT API to answer users’ questions.
//                                    Please answer the question below, and refer to the provided Context when you respond.
//                                    (Question: {question}),
//                                    (Context: {context})
//                                    """;

        var timeBegin = DateTime.Now;

        var answer = await OpenAiManager.GetChat(question);
        
        var answerId = Guid.NewGuid().ToString();

        var timeComplete = DateTime.Now;

        // var answerWithMarkdown = Markdown.ToHtml(answer).Replace("<p>", "").Replace("</p>", "");

        var answerHtml = $"""
                          <div style="color: #707070;">
                          {(timeComplete - timeBegin).TotalSeconds:N0}초 동안 조리
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

        var data = new
        {
            html = answerHtml,
            content = answer,
            answerId = $"answer-{answerId}",
        };

        return Json(data);
    }
}
