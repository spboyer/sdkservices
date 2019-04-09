using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace sdkservices
{
  class Program
  {
    static void Main(string[] args)
    {
      //Instantiate DI container for the application
      var serviceCollection = new ServiceCollection();

      //Register NodeServices
      serviceCollection.AddNodeServices(options => options.InvocationTimeoutMilliseconds = 240 * 1000);

      //Request the DI container to supply the shared INodeServices instance
      var serviceProvider = serviceCollection.BuildServiceProvider();
      var nodeService = serviceProvider.GetRequiredService<INodeServices>();

      var taskResult = Login(nodeService);

      Task.WaitAll(taskResult);

      if (taskResult.IsCompletedSuccessfully)
      {
        Console.WriteLine(taskResult.Result);
        Console.WriteLine();
        
        var auth = JsonConvert.DeserializeObject<AuthResult>(taskResult.Result);

        var customTokenProvider = new AzureCredentials(
                            new TokenCredentials(auth.token),
                            new TokenCredentials(auth.token),
                            auth.tenantId,
                            AzureEnvironment.AzureGlobalCloud);

        var client = RestClient
                      .Configure()
                      .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                      .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                      .WithCredentials(customTokenProvider)
                      .Build();

        var authenticatedClient = Azure.Authenticate(client, auth.tenantId).WithDefaultSubscription();

        Console.WriteLine(authenticatedClient.SubscriptionId);
      }
    }


    private static async Task<string> Login(INodeServices nodeService)
    {
      //Invoke the javascript module with parameters to execute in Node environment.
      return await nodeService.InvokeAsync<string>(@"scripts/auth.js");
    }

  }
}
