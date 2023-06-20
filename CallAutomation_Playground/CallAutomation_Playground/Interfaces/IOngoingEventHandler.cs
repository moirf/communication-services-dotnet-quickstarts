namespace CallAutomation_Playground.Interfaces
{
    public interface IOngoingEventHandler
    {
        void AttachCountParticipantsInTheCall(string callConnectionId);

        void AttachDisconnectedWrapup(string callConnectionId);
        //void AaddParticipantToCall(string callConnectionId, string target);
    }
}
