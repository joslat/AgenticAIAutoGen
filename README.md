# AgenticAIAutoGen
Demos and Slides for my AI Agents Talk at .NET Day Switzerland

## Slides
Agentic AI -Unleash Your AI Potential with AutoGen - final.pdf
Note I have updated the perception on agents at the beginning of the talk and at the end (using Menti)

## Code
### A01_MultiAgent_Critic_ToM_Argument_CheatSheet.cs
The first demo showcases a writer-critic workflow applying ToM to create an Argumentary and polish it
through subsequent reviews. 
Use it for discussing next year's .NET Day Switzerland!!! ;)
(...and let me know if it helped! Hint: it should!)

### A02_Reflection_with_Multi_Critic.cs
This second demo showcases the creation of what I'd like to call "committee of experts" and use 
in the context of a critic workflow, with a summarization and wrapped in a critic agent.
Also to highlight the value of the Groundedness check (I call it fact checker - FactChecker)
and it ensures that any hallucinations or facts it believes to be true (Gpt-4o mini) - as it 
has been trained up to Sept 2023 it knows of .NET Day Switzerland 2023 and earlier...
So you can see how it corrects itself and the substantial improvement this layer does. 

Again, thanks so much to LittleLittleCloud, Xiaoyun Zhang, for his help and curiosity - was
talking to him as I would introduce him and his efforts on the .NET side of AutoGen... and 
he was curious on the code and once I shared it, he provided a PR with improvements and we 
had a call over it with suggestions and ideas - which was very motivating.
Thanks Xiaoyun! 

You can check his valuable GitHub at https://github.com/LittleLittleCloud

