using EdsLibrary.Extensions;
using EdsLibrary.Logging.Table;
using Genetika.Genetic;
using Genetika.Genetic.Fitness;
using Genetika.Interfaces;
using System;
using System.Drawing;
using System.IO;

namespace EvolveNNExamples.Velocity
{
    public class ImageEntity : IEntity<ImageEntity>
    {
        public StudImageContainer studImageContainer;
        public int widthPixels;
        public int heightPixels;
        public int totalPixels;

        private readonly int numberOfInputs;
        public int NumberOfInputs => numberOfInputs;
        public int NumberOfOutputs => 1;
        public bool Active { get; set; } = false;
        private float fitness = 0;
        public float Fitness { get => fitness / updates; set => fitness = value; }

        public int Id => throw new NotImplementedException();

        public bool AccumulativeFitness => throw new NotImplementedException();

        public FitnessLogic FitnessLogic => throw new NotImplementedException();

        private int updates = 0;
        private StudImage lastImage = null;

        public ImageEntity(StudImageContainer stubImageContainer)
        {
            this.studImageContainer = stubImageContainer;
            this.numberOfInputs = stubImageContainer.studImages[0].image.Height * stubImageContainer.studImages[0].image.Width;
        }

        public void OnUpdate(float[] outputs)
        {
            if (outputs[0] >= 0.5f && lastImage.isStud) fitness += 1;
            else if (outputs[0] < 0.5f && !lastImage.isStud) fitness += 1;
            updates++;
        }

        public float[] GenerateInput()
        {
            lastImage = studImageContainer.GetRandom();
            return lastImage.value;
        }

        public void OnRestart() { updates = 0; }

        public void OnActivate() { }
        public void OnDeactivate() { }

        public float GetFitness()
        {
            throw new NotImplementedException();
        }

        public void Update(float[] outputs)
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public TableFormatter GetTableFormatter()
        {
            throw new NotImplementedException();
        }

        public TableFormatter GetTableFormatter(ImageEntity compare)
        {
            throw new NotImplementedException();
        }
    }
}
