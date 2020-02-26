using System.Collections.Generic;
using State = SmsSync.Models.OutboxNotification.NotificationState;

namespace SmsSync.Services
{
    public static class StatesHelper
    {
        private static readonly Dictionary<State, State> PromoteStates =
            new Dictionary<State, State>
            {
                [State.New] = State.WaitForSend,
                [State.WaitForSend] = State.Sent,
                [State.Sent] = State.WaitForCommit,
                [State.WaitForCommit] = State.Committed,
                [State.Committed] = State.WaitForRemove,
                [State.WaitForRemove] = State.WaitForRemove,
                [State.Failed] = State.WaitForMark,
                [State.WaitForMark] = State.Marked,
                [State.Marked] = State.WaitForRemoveFail,
                [State.WaitForRemoveFail] = State.WaitForRemoveFail
            };
        
        private static readonly Dictionary<State, State> RollbackStates =
            new Dictionary<State, State>
            {
                [State.New] = State.New,
                [State.WaitForSend] = State.Failed,
                [State.Sent] = State.Sent,
                [State.WaitForCommit] = State.Sent,
                [State.Committed] = State.Committed,
                [State.WaitForRemove] = State.Committed,
                [State.Failed] = State.Failed,
                [State.WaitForMark] = State.Failed,
                [State.Marked] = State.Marked,
                [State.WaitForRemoveFail] = State.Marked
            };

        public static State Promote(this State state)
        {
            return PromoteStates[state];
        }
        
        public static State Rollback(this State state)
        {
            return RollbackStates[state];
        }
    }
}