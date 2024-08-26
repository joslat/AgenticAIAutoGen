using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;
using FluentAssertions;
using System.Management.Automation;

namespace AutoGen001;

// NOTE: Originally based on the example code from LittleLittleCloud at
// https://github.com/LittleLittleCloud/AI-Agentic-Design-Patterns-with-AutoGen.Net/blob/main/L1_MultiAgent_Conversation_and_Standup_Comedy/Program.cs
public static class A01_MultiAgent_Critic_ToM_Argument_CheatSheet
{
    public async static Task Execute()
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Please set the OPENAI_API_KEY environment variable.");
        var openAIModel = "gpt-4o-mini";
        var openaiClient = new OpenAIClient(openAIKey);

        var tokenCountMiddleware = new CountTokenMiddleware();
        var openaiMessageConnector = new OpenAIChatRequestMessageConnector();

        await PrintTitle();

        // Company Description
        var companyDescription = "A Zurich-based IT Services Consultancy specializing in cloud solutions, cybersecurity, and software development. Known for its innovative approach, high-quality service, and collaborative environment.";

        // Employee (Sheila) Profile
        var workerProfile = "Sheila is a Senior Software Developer with 7 years of experience at the company. She is known for her strong technical skills, attention to detail, and passion for continuous learning. Sheila is proactive and values professional development opportunities, particularly those that keep her at the cutting edge of technology.";

        // Team Lead (John) Profile
        var teamLeadProfile = "John is a seasoned IT Team Lead with 12 years of experience. He is pragmatic, risk-averse, and focused on efficiency. John values ROI and tends to be skeptical about non-essential expenditures. He is known for his high standards and can be hard to convince without solid evidence or a clear benefit to the team and company.";

        // Conference Summary
        var conferenceSummary = @"
            .NET Day Switzerland is an independent technology conference taking place on Tuesday, 27.08.2024, at Arena Cinemas in Sihlcity, Zürich. It is designed for developers, architects, and experts to explore and discuss .NET technologies, including .NET, .NET Core, C#, ASP.NET Core, Azure, and more. The event features experienced speakers who provide deep insights into the latest Microsoft software development topics and beyond. In addition to technical talks, the conference offers opportunities for networking and discussions with speakers and other attendees.

            The conference is a non-profit community event, with all speakers and staff participating voluntarily to support the Swiss .NET community. Any surplus from ticket sales is donated to charity projects or to support the Swiss software developer community.

            The agenda includes a mix of introductory, intermediate, and advanced sessions, covering topics such as AI-driven software development, .NET MAUI, distributed systems, minimal APIs, and modern C# features. With sessions led by industry experts like Maddy Montaquila from Microsoft and Jose Luis Latorre Millas, a Microsoft MVP, the conference is an excellent opportunity for professional growth and networking.

            The event is not just about learning but also about engaging with the community, with opportunities to connect with peers during breaks and networking sessions.
            ";

        // Setup ArgumentaryWriter Agent
        var argumentaryWriter = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "ArgumentaryWriter",
            modelName: openAIModel,
            systemMessage: $"""
                Your name is ArgumentaryWriter, a Persuasiveness Expert. 
                Your task is to create a convincing argumentary document, to have a spoken in-person conversation, from Sheila's point of view to persuade her Team Lead, John, to allow her to attend a conference. 
                Worker profile: {workerProfile}
                Company Description: {companyDescription}
                Team Lead profile: {teamLeadProfile}
                Conference Summary: {conferenceSummary}
                Write the argumentary with ideas to start and lead the conversation, including at the end, a list of several possible questions from John and perfect responses to them from Sheila.
                Also, take any feedback from CriticTeamLead seriously and make necessary changes to the argumentary.
                """,
            temperature: 0.6f,
            maxTokens: 1200)
            .RegisterMiddleware(tokenCountMiddleware)
            .RegisterMiddleware(openaiMessageConnector)
            .RegisterPrintMessage();

        // Setup CriticTeamLead Agent
        var criticTeamLead = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "CriticTeamLead",
            modelName: openAIModel,
            systemMessage: $"""
                Your name is CriticTeamLead, an expert in understanding team dynamics and leadership perspectives.
                Your task is to critique the argument written by ArgumentaryWriter from the perspective of John, Sheila's Team Lead.
                Worker profile: {workerProfile}
                Company Description: {companyDescription}
                Team Lead profile: {teamLeadProfile}
                Conference Summary: {conferenceSummary}
                Provide suggestions to improve the argumentary to make it more persuasive and complete. When the argumentary is solid, and there is no suggestion to improve it in excess, respond with 'TERMINATE'.
                If the Argumentary is already solid and convincing, like 80-90% perfect, you can respond with 'TERMINATE' only.
                Do not write any part of the argumentary, even for reference. Only provide feedback and suggestions if any.
                If you provide ANY feedback, DO NOT, I repeat, DO NOT respond or add 'TERMINATE' in your feedback.
                After having replied 6 times, respond with 'TERMINATE' to end the conversation.
                AGAIN DO NOT WRITE ANY PART OF THE ARGUMENTARY. ONLY PROVIDE FEEDBACK.
                IF THE ARGUMENTARY IS SOLID, RESPOND WITH 'TERMINATE'.
                RESPOND WITH TERMINATE AFTER 6 REPLIES.
                """,
            temperature: 0.3f,
            maxTokens: 1200)
            .RegisterMiddleware(tokenCountMiddleware)
            .RegisterMiddleware(openaiMessageConnector)
            .RegisterPrintMessage();

        var chatHistory = new List<IMessage>
        {
            new TextMessage(Role.User, "Please create an argumentary document for Sheila to convince John to attend the conference.", from: criticTeamLead.Name)
        };

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("\nAssistant ArgumentaryWriter > \n");

        int i = 0;
        IMessage? lastMessageBeforeTerminate = null;
        try
        {
            await foreach (var msg in criticTeamLead.SendAsync(
                receiver: argumentaryWriter, 
                chatHistory, 
                maxRound: 16))
            {
                i++;
                Console.WriteLine($"Round {i}");
                chatHistory.Add(msg);
                
                if (msg.GetContent()?.ToLower().Contains("terminate") is true)
                {
                    lastMessageBeforeTerminate = chatHistory[^2]; // Take the last message before "TERMINATE"

                    break;
                }

                if (msg.From == "ArgumentaryWriter") // Next Message is from CriticTeamLead
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\nAssistant CriticTeamLead > \n");
                }
                else // Next Message is from ArgumentaryWriter
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\nAssistant ArgumentaryWriter > \n");
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
        
        // Write the final argumentary to a file
        if (lastMessageBeforeTerminate != null)
        {
            await File.WriteAllTextAsync(
                "Argumentary_CheatSheet.txt", 
                lastMessageBeforeTerminate.GetContent());
        }

    }

    public async static Task PrintTitle()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("""
                                       _____            __ _   
                /\                    / ____|          / _| |  
               /  \   _ __ __ _ _   _| |     _ __ __ _| |_| |_ 
              / /\ \ | '__/ _` | | | | |    | '__/ _` |  _| __|
             / ____ \| | | (_| | |_| | |____| | | (_| | | | |_ 
            /_/    \_\_|  \__, |\__,_|\_____|_|  \__,_|_|  \__|
                           __/ |                               
                          |___/                                      
                                          ArguCraft by José L. Latorre              
            """);
    }
}