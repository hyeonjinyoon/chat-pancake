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
            return Json(new { error = "No data provided" });

        var question = model.Data;
        
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
        
        var timeComplete = DateTime.Now;

        // var answerWithMarkdown = Markdown.ToHtml(answer).Replace("<p>", "").Replace("</p>", "");
        
        var answerHtml = $"""
                          <div style="color: #707070;">
                          {(timeComplete - timeBegin).TotalSeconds:N0}초 동안 조리
                          </div>
                          <div
                              id="answer"
                              style="margin-top: 8px;
                              margin-right: auto; 
                              width: fit-content; 
                              background-color: #303030; 
                              color: #ccc; padding: 9px; 
                              border-radius: 20px; 
                              max-width: 600px;
                              white-space: pre-wrap;
                              overflow: auto;
                              align-items: center;"><pre>{answer}</pre></div>
                          """;

        var data = new
        {
            html = answerHtml,
            content = answer,
        };
        
        return Json(data);
    }
}
