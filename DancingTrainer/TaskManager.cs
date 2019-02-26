using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DancingTrainer
{
    class TaskManager
    {

        Task t;
        CancellationTokenSource ts = new CancellationTokenSource();
        CancellationToken ctoken;

        public TaskManager(Action a)
        {
            ctoken = ts.Token;
            t = new Task(a,ctoken);
        }

    }
}
