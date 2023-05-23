using Azure.Communication;
using Azure.Communication.Identity;

namespace CallAutomation_AppointmentReminder
{
    public static class CallAutomationMediaHelper
    {
        public async static Task<string> ProvisionAzureCommunicationServicesIdentity(string connectionString)
        {
            var client = new CommunicationIdentityClient(connectionString);
            var user = await client.CreateUserAsync().ConfigureAwait(false);
            return user.Value.Id;
        }
    }
}
