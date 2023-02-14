using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Roverlib.Services
{
    public class RoverController
    {
        private readonly TrafficControlService gm;

        public RoverController(TrafficControlService gm)
        {
            this.gm = gm;
        }



        // public async Task ExecuteIngenuityPlan(Queue<(int X, int Y)> plan, CancellationToken token)
        // {
        //     while (plan.Count > 0)
        //     {
        //         if (token.IsCancellationRequested)
        //         {
        //             break;
        //         }
        //         var coord = plan.Peek();
        //         try
        //         {
        //             await gm.MoveIngenuityAsync(Convert.ToInt32(coord.X), Convert.ToInt32(coord.Y));
        //             var done = plan.Dequeue();
        //         }
        //         catch (Exception e)
        //         {
        //         }
        //     }
        // }

        // public async Task ExecutePerserverancePlan(Queue<(int X, int Y)> plan, CancellationToken token)
        // {
        //     while (plan.Count > 0)
        //     {
        //         if (token.IsCancellationRequested)
        //         {
        //             break;
        //         }
        //         var coord = plan.Peek();
        //         try
        //         {
        //             await gm.MovePerserveranceToPointAsync(Convert.ToInt32(coord.X), Convert.ToInt32(coord.Y));
        //             var done = plan.Dequeue();
        //         }
        //         catch { }
        //     }
        // }
    }

    public static class ListExtensions
    {
        public static Queue<(int x, int y)> ToQueue(this IEnumerable<(int x, int y)> points)
        {
            Queue<(int x, int y)> queue = new();
            foreach (var p in points)
            {
                queue.Enqueue(p);
            }
            return queue;
        }
    }

}
