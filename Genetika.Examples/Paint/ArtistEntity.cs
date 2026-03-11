using EdsLibrary.Logging.Table;
using Genetika.Genetic.Fitness;
using Genetika.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Genetika.Examples.Paint
{
    public class ArtistEntity : CanvasEntity, IEntity<ArtistEntity>
    {
        private static int Count = 0;

        /// <summary>
        /// Inputs that capture the "feel" of the machine:
        ///  0: positionX (normalized)
        ///  1: positionY (normalized)
        ///  2: painting progress (step / total steps estimate)
        ///  3: CPU/process time pressure (how busy the machine feels)
        ///  4: Memory pressure (working set as fraction of available)
        ///  5: GC stress (gen0 collections, normalized)
        ///  6: Thread pool heartbeat (available threads, normalized)
        ///  7: System uptime pulse (sine wave from tick count - a slow breathing rhythm)
        ///  8: Previous brush red
        ///  9: Previous brush green
        /// 10: Previous brush blue
        /// </summary>
        public const int NumberOfInputs = 11;

        /// <summary>
        /// Outputs that control the brush:
        ///  0: X movement
        ///  1: Y movement
        ///  2: Red
        ///  3: Green
        ///  4: Blue
        ///  5: Brush size
        ///  6: Opacity
        ///  7: Brush type selector
        ///  8: Pen pressure (below threshold = pen lifts, allowing repositioning without painting)
        /// </summary>
        public const int NumberOfOutputs = 9;

        // Cached system metrics (refreshed periodically to avoid overhead)
        private static long lastMetricTick = 0;
        private static float cachedCpuPressure = 0f;
        private static float cachedMemoryPressure = 0f;
        private static float cachedGcStress = 0f;
        private static float cachedThreadPulse = 0f;
        private static float cachedUptimeBreath = 0f;
        private static readonly object metricLock = new object();

        public ArtistEntity()
        {
            Id = Count++;
        }

        public int Id { get; private set; }

        public bool AccumulativeFitness => false;

        public FitnessLogic FitnessLogic => FitnessLogic.PreferHigher;

        public float GetFitness()
        {
            return JudgePainting;
        }

        public void Update(float[] outputs)
        {
            base.Update(
                outputs[0],  // xChange
                outputs[1],  // yChange
                outputs[2],  // red
                outputs[3],  // green
                outputs[4],  // blue
                outputs[5],  // brush size
                outputs[6],  // opacity
                outputs[7],  // brush type
                outputs[8]   // pen pressure
            );
        }

        public float[] GenerateInput()
        {
            RefreshSystemMetrics();

            float progress = stepCount / 10000f; // normalized progress

            return new float[]
            {
                positionX / canvasSize,       // 0: normalized position X
                positionY / canvasSize,       // 1: normalized position Y
                progress,                      // 2: painting progress
                cachedCpuPressure,             // 3: CPU pressure
                cachedMemoryPressure,          // 4: memory pressure
                cachedGcStress,                // 5: GC stress
                cachedThreadPulse,             // 6: thread pool heartbeat
                cachedUptimeBreath,            // 7: system breathing rhythm
                brushR,                        // 8: previous brush red
                brushG,                        // 9: previous brush green
                brushB,                        // 10: previous brush blue
            };
        }

        /// <summary>
        /// Samples the machine's vital signs to feed as artistic inspiration.
        /// Cached and refreshed every ~50ms to keep overhead low.
        /// </summary>
        private static void RefreshSystemMetrics()
        {
            long now = Environment.TickCount;
            if (Math.Abs(now - lastMetricTick) < 50) return;

            lock (metricLock)
            {
                if (Math.Abs(now - lastMetricTick) < 50) return;
                lastMetricTick = now;

                try
                {
                    // CPU pressure: how much processor time this process has consumed
                    // relative to wall-clock time, gives a 0-1 "business" feel
                    var proc = Process.GetCurrentProcess();
                    double cpuSeconds = proc.TotalProcessorTime.TotalSeconds;
                    double wallSeconds = (DateTime.Now - proc.StartTime).TotalSeconds;
                    cachedCpuPressure = (float)Math.Min(1.0, cpuSeconds / Math.Max(1.0, wallSeconds));

                    // Memory pressure: working set relative to a baseline
                    // Normalized so typical values land in 0-1 range
                    long workingSetMb = proc.WorkingSet64 / (1024 * 1024);
                    cachedMemoryPressure = (float)Math.Min(1.0, workingSetMb / 512.0);

                    // GC stress: generation 0 collection count, wrapped into 0-1 with sine
                    int gen0 = GC.CollectionCount(0);
                    cachedGcStress = (float)(Math.Sin(gen0 * 0.1) * 0.5 + 0.5);

                    // Thread pool heartbeat
                    ThreadPool.GetAvailableThreads(out int workerThreads, out int completionThreads);
                    ThreadPool.GetMaxThreads(out int maxWorker, out _);
                    float threadUsage = 1f - ((float)workerThreads / Math.Max(1, maxWorker));
                    cachedThreadPulse = threadUsage;

                    // System uptime breathing: a slow oscillation based on tick count
                    // Creates a gentle, organic rhythm unique to each machine's uptime
                    double uptimeSeconds = Environment.TickCount / 1000.0;
                    cachedUptimeBreath = (float)(Math.Sin(uptimeSeconds * 0.07) * 0.5 + 0.5);
                }
                catch
                {
                    // If any metric fails, keep the cached values
                }
            }
        }

        public TableFormatter GetTableFormatter()
        {
            TableFormatter formatter = new TableFormatter();
            formatter.Add("Id", Id, "0", 4);
            formatter.Add("X", positionX, "+0;-0", 4);
            formatter.Add("Y", positionY, "+0;-0", 4);
            formatter.Add("Px", pixelsTouched, "0", 6);
            formatter.Add("Dst", totalDistance, "0", 6);

            return formatter;
        }

        public TableFormatter GetTableFormatter(ArtistEntity compare)
        {
            TableFormatter formatter = new TableFormatter();
            formatter.Add("Id", new KeyValuePair<float, float>(Id, compare.Id), "0", 4);
            formatter.Add("X", new KeyValuePair<float, float>(positionX, compare.positionX), "+0;-0", 4);
            formatter.Add("Y", new KeyValuePair<float, float>(positionY, compare.positionY), "+0;-0", 4);
            formatter.Add("Px", new KeyValuePair<float, float>(pixelsTouched, compare.pixelsTouched), "0", 6);

            return formatter;
        }
    }
}
