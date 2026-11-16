using System;
using System.Collections.Generic;

namespace TinyGiantProblems
{
    public enum TaskKind { CareJob, ClearMess, SellDebris, FindTrinket, RecoverDrone, CalmGiant }
    public enum MessKind { Grime, Burr, Mite }

    [Serializable]
    public sealed class WorkOrder
    {
        public string title;
        public TaskKind kind;
        public int subtype = -1;
        public int target = 1, progress, bounty;
        public bool optional, paid;
        public bool Complete => progress >= target;
    }

    // Work orders pay Guild coins the moment they are completed.
    public sealed class TaskBoard
    {
        public readonly List<WorkOrder> orders = new List<WorkOrder>();
        public Action<WorkOrder> Completed;

        public static TaskBoard MossbackContract(IList<string> careJobTitles)
        {
            var board = new TaskBoard();
            int[] careBounty = { 40, 40, 60 };
            for (int i = 0; i < careJobTitles.Count; i++)
                board.orders.Add(new WorkOrder { title = careJobTitles[i], kind = TaskKind.CareJob, subtype = i, bounty = i < careBounty.Length ? careBounty[i] : 40 });
            board.orders.Add(new WorkOrder { title = "Scrub grime patches", kind = TaskKind.ClearMess, subtype = (int)MessKind.Grime, target = 8, bounty = 35 });
            board.orders.Add(new WorkOrder { title = "Pick burrs from the fur", kind = TaskKind.ClearMess, subtype = (int)MessKind.Burr, target = 6, bounty = 35 });
            board.orders.Add(new WorkOrder { title = "Vacuum roaming Bramblemites", kind = TaskKind.ClearMess, subtype = (int)MessKind.Mite, target = 5, bounty = 45 });
            board.orders.Add(new WorkOrder { title = "Sell salvage at the yard", kind = TaskKind.SellDebris, target = 12, bounty = 50 });
            board.orders.Add(new WorkOrder { title = "Find a hidden trinket", kind = TaskKind.FindTrinket, target = 1, bounty = 40, optional = true });
            board.orders.Add(new WorkOrder { title = "Recover the survey drone", kind = TaskKind.RecoverDrone, target = 1, bounty = 60, optional = true });
            board.orders.Add(new WorkOrder { title = "Feed Mossback a snack", kind = TaskKind.CalmGiant, target = 1, bounty = 25, optional = true });
            return board;
        }

        public int Report(TaskKind kind, int amount = 1, int subtype = -1)
        {
            int paid = 0;
            foreach (var o in orders)
            {
                if (o.kind != kind || o.paid) continue;
                if (o.subtype >= 0 && o.subtype != subtype) continue;
                o.progress = Math.Min(o.target, o.progress + amount);
                if (o.Complete) { o.paid = true; paid += o.bounty; Completed?.Invoke(o); }
            }
            return paid;
        }

        public int Earned { get { int n = 0; foreach (var o in orders) if (o.paid) n += o.bounty; return n; } }
        public int Remaining { get { int n = 0; foreach (var o in orders) if (!o.paid && !o.optional) n++; return n; } }
        public WorkOrder CareOrder(int jobIndex) => orders.Find(o => o.kind == TaskKind.CareJob && o.subtype == jobIndex);
    }
}
