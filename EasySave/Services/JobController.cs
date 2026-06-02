using System.Threading;

namespace EasySave.Services
{
    public class JobController
    {
        
        public string JobName { get; }
        public JobStatus Status { get; private set; }
            = JobStatus.Idle;

        // Pause : bloque le thread jusqu'à ce qu'on reprenne
        private readonly ManualResetEventSlim _pauseEvent
            = new ManualResetEventSlim(initialState: true);

        // Stop : signale au thread de s'arrêter
        private readonly CancellationTokenSource _cts
            = new CancellationTokenSource();

        public CancellationToken CancellationToken
            => _cts.Token;

        public JobController(string jobName)
        {
            JobName = jobName;
        }

        // Démarre ou reprend le travail
        public void Play()
        {
            Status = JobStatus.Running;
            _pauseEvent.Set(); 
        }

        
        public void Pause()
        {
            Status = JobStatus.Paused;
            _pauseEvent.Reset(); 
        }
        
        public void Stop()
        {
            Status = JobStatus.Stopped;
            _pauseEvent.Set();    
            _cts.Cancel();        
        }

        
        public void SetCompleted()
        {
            Status = JobStatus.Completed;
        }

        
        public void SetError()
        {
            Status = JobStatus.Error;
        }

        // Appelé avant chaque fichier : bloque si en pause, sort si arrêté
        public bool WaitIfPausedOrCancelled()
        {
            // Bloque jusqu'à Set() (Play) ou annulation
            _pauseEvent.Wait(_cts.Token);

            // Retourne false si l'annulation a été demandée
            return !_cts.Token.IsCancellationRequested;
        }
    }

    public enum JobStatus
    {
        Idle,       // pas encore démarré
        Running,    
        Paused,     
        Stopped,    
        Completed,  
        Error       
    }
}