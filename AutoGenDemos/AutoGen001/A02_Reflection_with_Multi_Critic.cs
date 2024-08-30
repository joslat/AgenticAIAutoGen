using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;
using FluentAssertions;

namespace AutoGen001;

// NOTE: Originally based on the example code from LittleLittleCloud at
// https://github.com/LittleLittleCloud/AI-Agentic-Design-Patterns-with-AutoGen.Net/tree/main/L3_Reflection_and_Blogpost_Writing
public static class A02_Reflection_with_Multi_Critic
{
    public async static Task Execute()
    {
        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Please set the OPENAI_API_KEY environment variable.");
        //
        var openAIModel = "gpt-4o-mini";
        var openaiClient = new OpenAIClient(openAIKey);

        // The Task!
        var ConferenceDescription = GetConferenceDescription();

        var task = $"""
            Write a concise but engaging 400-word article about 
            the ".NET Day Switzerland" conference for 2024. The article should cover
            the conference's purpose, key highlights, the non-profit nature of the event,
            and the diverse sessions offered. Make sure it appeals to developers, architects,
            and experts as well as Team Leads and CTO's and includes a strong and catchy title.
            Also highlight the topics covered, some speakers, and the community engagement aspect.
            """;

        // Create a writer agent
        var writer = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "Writer",
            modelName: openAIModel,
            systemMessage: """
                You are a writer. You write engaging and concise articles (with title) on given topics.
                You must polish your writing based on the feedback you receive and provide a refined version.
                Only return your final work without additional comments.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        // Adding reflection by creating a critic agent to reflect on the work of the writer agent.
        var critic = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "Critic",
            modelName: openAIModel,
            systemMessage: """
                You are a critic. You review the work of the writer and provide constructive feedback to help improve the quality of the content.
                If the work is already solid and convincing, like 80-90% perfect, you can respond with 'TERMINATE' only.
                If you provide ANY feedback, DO NOT, I repeat, DO NOT respond or add 'TERMINATE' in your feedback.
                After having replied 4 times, respond with 'TERMINATE' to end the conversation.
                AGAIN DO NOT WRITE ANY PART OF THE work. ONLY PROVIDE FEEDBACK.
                IF THE work IS SOLID, RESPOND WITH 'TERMINATE'.
                RESPOND WITH TERMINATE AFTER 4 REPLIES.                
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        // Nested chat using middleware pattern
        // Python AutoGen allows you to create a Json object task list and pass it to agent using `register_nested_chats`
        // This is not supported in C# AutoGen yet.
        // But you can achieve the same using the middleware pattern.

        var SEOReviewer = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "SEO_Reviewer",
            modelName: openAIModel,
            systemMessage: """
                You are an SEO reviewer, known for your ability to optimize content for search engines, ensuring that it ranks well and attracts organic traffic.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        var LegalReviewer = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "Legal_Reviewer",
            modelName: openAIModel,
            systemMessage: """
                You are a legal reviewer, known for your ability to ensure that content is legally compliant and free from any potential legal issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                Also be aware of data privacy and GDPR compliance which needs to be respected, so in doubt suggest the removal of PII information.
                Asume that the speakers have agreed to share their name and title, so there is no issue with sharing that in the article.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        var EthicsReviewer = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "Ethics_Reviewer",
            modelName: openAIModel,
            systemMessage: """
                You are an ethics reviewer, known for your ability to ensure that content is ethically sound and free from any potential ethical issues.
                Make sure your suggestion is concise (within 3 bullet points), concrete and to the point.
                Begin the review by stating your role.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        var FactChecker = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "FactChecker",
            modelName: openAIModel,
            systemMessage: $"""
                You are a FactChecker. Your job is to ensure that all facts mentioned in the article are accurate and derived from the provided source material.
                You will avoid any hallucinations and that all the facts are cross-checked with the source material.

                Cross-check the content against the following source:
                
                {ConferenceDescription}
                
                Make sure no invented information is included, and suggest corrections if any discrepancies are found.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        var StyleChecker = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "StyleChecker",
            modelName: openAIModel,
            systemMessage: """
                You are a Style Checker. Your task is to ensure that the article is written in a proper style. 
                Check that the writing style is positive, engaging, motivational, original, and funny. 
                The phrases should not be too complex, and the tone should be friendly, casual, yet polite.
                Provide suggestions to improve the style if necessary.
                Provide ONLY Suggestions, do not rewrite the content or write any part of the content in the style suggested, this is the work of the writer.
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        var MetaReviewer = new OpenAIChatAgent(
            openAIClient: openaiClient,
            name: "Meta_Reviewer",
            modelName: openAIModel,
            systemMessage: """
                You are a meta reviewer, you aggregate and review the work of other reviewers and give a final suggestion on the content."
                """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        // Orchestrate the nested chats to solve the task
        var middleware = new NestChatMultiCriticMiddleware(
            writer: writer,
            seo: SEOReviewer,
            legal: LegalReviewer,
            ethics: EthicsReviewer,
            meta: MetaReviewer,
            factchecker: FactChecker,
            stylechecker: StyleChecker);

        var nestedChatCritic = critic
            .RegisterMiddleware(middleware)
            .RegisterPrintMessage();

        
        var conversation = await nestedChatCritic.SendAsync(
            message: task,
            receiver: writer,
            maxRound: 5)
            .ToListAsync();

        var finalResult = conversation.Last();
    }

    private static string GetConferenceDescription()
    {
        var conferenceSummary = @"
            .NET Day Switzerland is an independent technology conference taking place on Tuesday, 27.08.2024, at Arena Cinemas in Sihlcity, Zürich. It is designed for developers, architects, and experts to explore and discuss .NET technologies, including .NET, .NET Core, C#, ASP.NET Core, Azure, and more. The event features experienced speakers who provide deep insights into the latest Microsoft software development topics and beyond. In addition to technical talks, the conference offers opportunities for networking and discussions with speakers and other attendees.

            The conference is a non-profit community event, with all speakers and staff participating voluntarily to support the Swiss .NET community. Any surplus from ticket sales is donated to charity projects or to support the Swiss software developer community.

            The agenda includes a mix of introductory, intermediate, and advanced sessions, covering topics such as AI-driven software development, .NET MAUI, distributed systems, minimal APIs, and modern C# features. With sessions led by industry experts like Maddy Montaquila from Microsoft and Jose Luis Latorre Millas, a Microsoft MVP, the conference is an excellent opportunity for professional growth and networking.

            The event is not just about learning but also about engaging with the community, with opportunities to connect with peers during breaks and networking sessions.

            ### Full Agenda:

            1. **It's not your dad's .NET anymore**
               - **Time**: 08:30 - 09:40 CINEMA 3
               - **Speaker**: Maddy Montaquila, Senior Product Manager, .NET MAUI at Microsoft

            2. **Agentic AI: Unleash Your AI Potential with AutoGen**
               - **Time**: 10:10 - 10:55 Intermediate CINEMA 3
               - **Speaker**: Jose Luis Latorre Millas, Microsoft MVP & International Speaker

            3. **Be Sharp with New Features in C#**
               - **Time**: 10:10 - 10:55 Intermediate CINEMA 2
               - **Speaker**: Roland Guijt, Microsoft MVP, Pluralsight author, ASP.NET insider

            4. **Let's turn your website into a hybrid .NET MAUI app**
               - **Time**: 10:10 - 10:55 Intermediate CINEMA 6
               - **Speaker**: Mark Allibone, Technical Lead at Rey Technology, Microsoft MVP

            5. **Evolutionary Architecture: The What. The Why. The How.**
               - **Time**: 10:10 - 10:55 Intermediate CINEMA 8
               - **Speaker**: Maciej 'MJ' Jedrzejewski, Fractional architect, consultant, advisor

            6. **Making the best out of AI in your daily working life**
               - **Time**: 11:05 - 11:50 Intermediate CINEMA 3
               - **Speaker**: Laurent Bugnion, Microsoft Cloud Developer Advocate

            7. **100% Unit Test Coverage and beyond**
               - **Time**: 11:05 - 11:50 Advanced CINEMA 2
               - **Speaker**: Marc Sallin, Solution Architect at Swiss Post

            8. **Web/App/Desktop mit Blazor – One to rule them all!**
               - **Time**: 11:05 - 11:50 Intermediate CINEMA 6
               - **Speaker**: Christian Giesswein, CEO

            9. **Five common mistakes with distributed systems**
               - **Time**: 11:05 - 11:50 Intermediate CINEMA 8
               - **Speaker**: Adam Ralph, Distributed systems expert at Particular Software, Microsoft MVP

            10. **AI-driven Software Development with Azure AI**
                - **Time**: 12:50 - 13:35 Introductory CINEMA 2
                - **Speaker**: Jörg Neumann, NeoGeeks GmbH

            11. **Building minimal APIs from scratch**
                - **Time**: 12:50 - 13:35 Intermediate CINEMA 6
                - **Speaker**: Safia Abdalla, Software Engineer on the ASP.NET Core team

            12. **Backend for Frontend (BFF) as a Gateway to the World of Microservices**
                - **Time**: 12:50 - 13:35 Intermediate CINEMA 8
                - **Speaker**: Daniel Murrmann, .NET Developer, fancy Development

            13. **Implementing the planet's largest e-commerce site using service boundaries**
                - **Time**: 12:50 - 13:35 Expert CINEMA 3
                - **Speaker**: Dennis van der Stelt, Distributed Systems addict

            14. **Experience new ways to code with C# and AI**
                - **Time**: 13:45 - 14:30 Intermediate CINEMA 3
                - **Speaker**: Rachel Appel, Developer Advocate at JetBrains

            15. **From Task.Run to Task.WhenAll: The Good, The Bad, and The Async**
                - **Time**: 13:45 - 14:30 Intermediate CINEMA 2
                - **Speaker**: Steven Giesel, Microsoft MVP / .NET Software Engineer

            16. **Real-Time Connected Apps with .NET MAUI, Blazor and SignalR**
                - **Time**: 13:45 - 14:30 Intermediate CINEMA 6
                - **Speaker**: Gerald Versluis, Senior Software Engineer at Microsoft

            17. **Onion, Hexagonal, Clean or Fractal Architecture? All of them, and more!!**
                - **Time**: 13:45 - 14:30 Intermediate CINEMA 8
                - **Speaker**: Urs Enzler, Software Architect @ Calitime AG

            18. **Sitting in meetings all day long: My first 180 days as a new Software Engineering Manager**
                - **Time**: 15:00 - 15:45 Introductory CINEMA 3
                - **Speaker**: Dennis Dietrich, Manager Software Development ICS, Phoenix Contact

            19. **Mastering Integration Testing for .NET Web APIs with WebApplicationFactory and TestContainers**
                - **Time**: 15:00 - 15:45 Advanced CINEMA 2
                - **Speaker**: Marc Rufer, Senior Software Engineer @isolutions | Microsoft MVP

            20. **Demystify cloud-native development with .NET Aspire**
                - **Time**: 15:00 - 15:45 Intermediate CINEMA 6
                - **Speaker**: Maddy Montaquila, Senior Product Manager, .NET MAUI at Microsoft

            21. **Curious Code and where to find it**
                - **Time**: 15:00 - 15:45 Introductory CINEMA 8
                - **Speaker**: Alexander Kayed, Senior Software Engineer, Noser Engineering AG

            22. **Growing and Thriving as an Engineering Manager**
                - **Time**: 15:55 - 17:05 CINEMA 3
                - **Speaker**: Taylor Poindexter, Spotify - Engineering Manager II
            ";

        return conferenceSummary;
    }
}

class NestChatMultiCriticMiddleware : IMiddleware
{
    private readonly IAgent seo;
    private readonly IAgent legal;
    private readonly IAgent ethics;
    private readonly IAgent meta;
    private readonly IAgent writer;
    private readonly IAgent factchecker;
    private readonly IAgent stylechecker;

    public NestChatMultiCriticMiddleware(
        IAgent writer, 
        IAgent seo, 
        IAgent legal, 
        IAgent ethics, 
        IAgent meta,
        IAgent factchecker,
        IAgent stylechecker)
    {
        this.writer = writer;
        this.seo = seo;
        this.legal = legal;
        this.ethics = ethics;
        this.meta = meta;
        this.factchecker = factchecker;
        this.stylechecker = stylechecker;
    }

    public string? Name => nameof(NestChatMultiCriticMiddleware);

    public async Task<IMessage> InvokeAsync(
        MiddlewareContext context, 
        IAgent critic, 
        CancellationToken cancellationToken = default)
    {
        // trigger only when the last message is from writer
        if (context.Messages.Last().From != writer.Name)
        {
            return await critic.GenerateReplyAsync(
                context.Messages, 
                context.Options, 
                cancellationToken);
        }

        var messageToReview = context.Messages.Last();
        var reviewPrompt = $"""
            Review the following content.

            {messageToReview.GetContent()}
            """;

        var seoReview = await critic.SendAsync(
            receiver: seo,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync();
        var legalReview = await critic.SendAsync(
            receiver: legal,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync();

        var ethicsReview = await critic.SendAsync(
            receiver: ethics,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync();

        var factsReview = await critic.SendAsync(
            receiver: factchecker,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync();

        var styleReview = await critic.SendAsync(
            receiver: stylechecker,
            message: reviewPrompt,
            maxRound: 1)
            .ToListAsync();

        var reviews = seoReview
            .Concat(legalReview)
            .Concat(ethicsReview)
            .Concat(factsReview)
            .Concat(styleReview);

        var metaReview = await critic.SendAsync(
            receiver: meta,
            message: "Aggregrate feedback from all reviewers and give final suggestions on the writing.",
            chatHistory: reviews,
            maxRound: 1)
            .ToListAsync();

        var lastReview = metaReview.Last();
        lastReview.From = critic.Name;

        return lastReview;
    }
}
