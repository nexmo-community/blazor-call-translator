# Blazor Call Translator

<img src="https://developer.nexmo.com/assets/images/Vonage_Nexmo.svg" height="48px" alt="Nexmo is now known as Vonage" />

This project lets you translate a PSTN call with the Vonage Voice API and Azure Cognitive Services.

## Welcome to Vonage

<!-- change "github-repo" at the end of the link to be the name of your repo, this helps us understand which projects are driving signups so we can do more stuff that developers love -->

If you're new to Vonage, you can [sign up for a Vonage API account](https://dashboard.nexmo.com/sign-up?utm_source=DEV_REL&utm_medium=github&utm_campaign=blazor-call-translator) and get some free credit to get you started.

## Prerequisites

* You'll need a Vonage API Account. If you don't have one, you can sign up for one [here](https://dashboard.nexmo.com/sign-up). Take note of your accounts Api Key, Api Secret, and the number that comes with it.
* You'll need an Azure Speech Resource - you can create one following the steps [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/overview#create-the-azure-resource) Pull the region and key value from the `Keys and Enpoint` tab on your resource.
* The latest [.NET Core SDK installed](https://dotnet.microsoft.com/download)
* [Visual Studio](https://aka.ms/vs) or [Visual Studio Code](https://aka.ms/vscode). I will be using [Visual Studio 2019](https://visualstudio.microsoft.com/vs/) for this demo
* This assumes that you've already procured a number, and linked it to an application that is pointing at a valid endpoint for Voange to reach. See the Voice API tab of our [Setting webhook endpoints](https://developer.nexmo.com/concepts/guides/webhooks#setting-webhook-endpoints) docs for more details

## Configure the app

In your `appsettings.json` file, set `SUBSCRIPTION_KEY` and `REGION` to your Azure resources subscription key and region

## Run the App

Run the app with `dotnet run`

## Getting Help

We love to hear from you so if you have questions, comments or find a bug in the project, let us know! You can either:

* Open an issue on this repository
* Tweet at us! We're [@VonageDev on Twitter](https://twitter.com/VonageDev)
* Or [join the Vonage Developer Community Slack](https://developer.nexmo.com/community/slack)

## Further Reading

* Check out the Developer Documentation at <https://developer.nexmo.com>

<!-- add links to the api reference, other documentation, related blog posts, whatever someone who has read this far might find interesting :) -->

