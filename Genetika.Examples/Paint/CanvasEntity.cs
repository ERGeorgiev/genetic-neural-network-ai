namespace Genetika.Examples.Paint
{
    public class CanvasEntity
    {
        public static readonly int canvasSize = 256;

        // RGBA canvas: [x][y][channel] where channels are R=0, G=1, B=2, A=3
        public byte[][][] canvas;
        public float positionX;
        public float positionY;
        public float brushR = 0.5f;
        public float brushG = 0.5f;
        public float brushB = 0.5f;
        public float brushSize = 2f;
        public float brushOpacity = 0.5f;
        public int stepCount = 0;

        // Painting statistics for fitness evaluation
        public int pixelsTouched = 0;
        public float totalDistance = 0f;
        public int strokeSegments = 0;

        public CanvasEntity()
        {
            canvas = new byte[canvasSize][][];
            for (int i = 0; i < canvasSize; i++)
            {
                canvas[i] = new byte[canvasSize][];
                for (int j = 0; j < canvasSize; j++)
                {
                    canvas[i][j] = new byte[4]; // RGBA, starts black transparent
                }
            }
            positionX = canvasSize / 2f;
            positionY = canvasSize / 2f;
        }

        public float JudgePainting
        {
            get
            {
                // Coverage: what fraction of the canvas has paint on it
                int coveredPixels = 0;
                float totalBrightness = 0f;
                // Track color variety via channel histograms (8 bins each)
                int[] rHist = new int[8];
                int[] gHist = new int[8];
                int[] bHist = new int[8];
                // Spatial distribution: divide canvas into a grid and count filled quadrants
                int gridSize = 8;
                int cellSize = canvasSize / gridSize;
                int[,] gridOccupancy = new int[gridSize, gridSize];
                // Per-cell color averages for local contrast measurement
                float[,] cellAvgR = new float[gridSize, gridSize];
                float[,] cellAvgG = new float[gridSize, gridSize];
                float[,] cellAvgB = new float[gridSize, gridSize];

                // Local smoothness: measure how similar neighboring painted pixels are
                float localSmoothness = 0f;
                int smoothnessSamples = 0;

                for (int x = 0; x < canvasSize; x++)
                {
                    for (int y = 0; y < canvasSize; y++)
                    {
                        byte a = canvas[x][y][3];
                        if (a > 0)
                        {
                            coveredPixels++;
                            byte r = canvas[x][y][0];
                            byte g = canvas[x][y][1];
                            byte b = canvas[x][y][2];
                            totalBrightness += (r + g + b) / (3f * 255f);

                            rHist[r / 32]++;
                            gHist[g / 32]++;
                            bHist[b / 32]++;

                            int gx = Math.Min(x / cellSize, gridSize - 1);
                            int gy = Math.Min(y / cellSize, gridSize - 1);
                            gridOccupancy[gx, gy]++;
                            cellAvgR[gx, gy] += r;
                            cellAvgG[gx, gy] += g;
                            cellAvgB[gx, gy] += b;

                            // Sample local smoothness (check right and down neighbors)
                            if (x + 1 < canvasSize && canvas[x + 1][y][3] > 0)
                            {
                                float dr = (r - canvas[x + 1][y][0]) / 255f;
                                float dg = (g - canvas[x + 1][y][1]) / 255f;
                                float db = (b - canvas[x + 1][y][2]) / 255f;
                                localSmoothness += 1f - Math.Min(1f, (float)Math.Sqrt(dr * dr + dg * dg + db * db));
                                smoothnessSamples++;
                            }
                            if (y + 1 < canvasSize && canvas[x][y + 1][3] > 0)
                            {
                                float dr = (r - canvas[x][y + 1][0]) / 255f;
                                float dg = (g - canvas[x][y + 1][1]) / 255f;
                                float db = (b - canvas[x][y + 1][2]) / 255f;
                                localSmoothness += 1f - Math.Min(1f, (float)Math.Sqrt(dr * dr + dg * dg + db * db));
                                smoothnessSamples++;
                            }
                        }
                    }
                }

                int totalPixels = canvasSize * canvasSize;
                float coverage = (float)coveredPixels / totalPixels;

                // Penalize both extremes: sweet spot around 15-65%
                float coverageScore;
                if (coverage < 0.05f)
                    coverageScore = coverage * 4f;
                else if (coverage <= 0.65f)
                    coverageScore = 1f;
                else
                    coverageScore = Math.Max(0f, 1f - (coverage - 0.65f) * 2f);

                // Color variety: Shannon entropy across the histograms
                float colorEntropy = ChannelEntropy(rHist, coveredPixels)
                                   + ChannelEntropy(gHist, coveredPixels)
                                   + ChannelEntropy(bHist, coveredPixels);
                float maxEntropy = 3f * (float)Math.Log(8);
                float colorScore = coveredPixels > 0 ? colorEntropy / maxEntropy : 0f;

                // Spatial spread: what fraction of grid cells are occupied
                int occupiedCells = 0;
                for (int gx = 0; gx < gridSize; gx++)
                    for (int gy = 0; gy < gridSize; gy++)
                        if (gridOccupancy[gx, gy] > 0)
                            occupiedCells++;
                float spreadScore = (float)occupiedCells / (gridSize * gridSize);

                // Local contrast between grid cells: reward paintings where
                // neighboring cells have different colors (compositional variety)
                float localContrast = 0f;
                int contrastPairs = 0;
                for (int gx = 0; gx < gridSize; gx++)
                {
                    for (int gy = 0; gy < gridSize; gy++)
                    {
                        if (gridOccupancy[gx, gy] == 0) continue;
                        float avgR1 = cellAvgR[gx, gy] / gridOccupancy[gx, gy];
                        float avgG1 = cellAvgG[gx, gy] / gridOccupancy[gx, gy];
                        float avgB1 = cellAvgB[gx, gy] / gridOccupancy[gx, gy];

                        // Check right and down neighbors
                        int[] dxArr = { 1, 0 };
                        int[] dyArr = { 0, 1 };
                        for (int d = 0; d < 2; d++)
                        {
                            int nx = gx + dxArr[d];
                            int ny = gy + dyArr[d];
                            if (nx >= gridSize || ny >= gridSize || gridOccupancy[nx, ny] == 0) continue;
                            float avgR2 = cellAvgR[nx, ny] / gridOccupancy[nx, ny];
                            float avgG2 = cellAvgG[nx, ny] / gridOccupancy[nx, ny];
                            float avgB2 = cellAvgB[nx, ny] / gridOccupancy[nx, ny];
                            float diff = (Math.Abs(avgR1 - avgR2) + Math.Abs(avgG1 - avgG2) + Math.Abs(avgB1 - avgB2)) / (3f * 255f);
                            localContrast += diff;
                            contrastPairs++;
                        }
                    }
                }
                float contrastScore = contrastPairs > 0 ? Math.Min(1f, localContrast / contrastPairs * 4f) : 0f;

                // Local smoothness: reward smooth color transitions within strokes
                // (high = colors change gradually, not randomly)
                float smoothnessScore = smoothnessSamples > 0 ? localSmoothness / smoothnessSamples : 0f;

                // Negative space: reward paintings that have a balance of
                // painted and unpainted areas with interesting boundaries
                // Measure occupancy variance across grid cells (some full, some empty = interesting)
                float avgOccupancy = (float)coveredPixels / (gridSize * gridSize);
                float occupancyVariance = 0f;
                for (int gx = 0; gx < gridSize; gx++)
                    for (int gy = 0; gy < gridSize; gy++)
                        occupancyVariance += (gridOccupancy[gx, gy] - avgOccupancy) * (gridOccupancy[gx, gy] - avgOccupancy);
                occupancyVariance /= (gridSize * gridSize);
                // Normalize: high variance means some cells are dense, others sparse
                float maxVariance = avgOccupancy * avgOccupancy;
                float compositionScore = maxVariance > 0 ? Math.Min(1f, occupancyVariance / maxVariance) : 0f;

                // Combine scores (higher is better)
                float score = coverageScore * 0.15f
                            + colorScore * 0.15f
                            + spreadScore * 0.10f
                            + contrastScore * 0.15f
                            + smoothnessScore * 0.20f
                            + compositionScore * 0.15f
                            + (strokeSegments > 2 ? 0.10f : strokeSegments * 0.05f); // reward having distinct strokes

                return score;
            }
        }

        private static float ChannelEntropy(int[] histogram, int total)
        {
            if (total == 0) return 0f;
            float entropy = 0f;
            for (int i = 0; i < histogram.Length; i++)
            {
                if (histogram[i] > 0)
                {
                    float p = (float)histogram[i] / total;
                    entropy -= p * (float)Math.Log(p);
                }
            }
            return entropy;
        }

        /// <summary>
        /// Update the artist's position and paint a stroke from the old to the new position.
        /// </summary>
        /// <param name="xChange">Horizontal movement delta</param>
        /// <param name="yChange">Vertical movement delta</param>
        /// <param name="r">Red channel 0-1</param>
        /// <param name="g">Green channel 0-1</param>
        /// <param name="b">Blue channel 0-1</param>
        /// <param name="size">Brush radius</param>
        /// <param name="opacity">Stroke opacity 0-1</param>
        /// <param name="brushType">0-1 selects brush type: soft round, hard round, splatter</param>
        /// <param name="penPressure">Pen pressure - below threshold the brush lifts (repositions without painting)</param>
        public virtual void Update(float xChange, float yChange, float r, float g, float b,
                                   float size, float opacity, float brushType, float penPressure)
        {
            stepCount++;

            // Pen pressure: sigmoid maps to 0-1, paint only when above threshold
            float pressure = Sigmoid(penPressure);
            bool penDown = pressure > 0.4f;

            // Clamp color channels to 0-1, then blend with previous color for smooth transitions
            float colorBlend = 0.6f; // how much of the new color to use (rest is previous)
            r = Clamp01(Sigmoid(r)) * colorBlend + brushR * (1f - colorBlend);
            g = Clamp01(Sigmoid(g)) * colorBlend + brushG * (1f - colorBlend);
            b = Clamp01(Sigmoid(b)) * colorBlend + brushB * (1f - colorBlend);
            opacity = Clamp01(Sigmoid(opacity) * 0.8f + 0.05f); // range ~0.05 to 0.85
            // Scale opacity by pen pressure for pressure-sensitive painting
            opacity *= (0.3f + 0.7f * pressure);
            size = 1f + Clamp01(Sigmoid(size)) * 20f; // brush radius 1-21 pixels

            float oldX = positionX;
            float oldY = positionY;

            // Movement: use sigmoid to bound delta, then scale for smooth motion
            float moveMagnitude = 2f + Clamp01(Sigmoid(Math.Abs(xChange) + Math.Abs(yChange))) * (canvasSize * 0.15f);
            float moveAngleX = (float)Math.Tanh(xChange);
            float moveAngleY = (float)Math.Tanh(yChange);
            positionX += moveAngleX * moveMagnitude;
            positionY += moveAngleY * moveMagnitude;

            // Wrap around canvas edges
            positionX = ((positionX % canvasSize) + canvasSize) % canvasSize;
            positionY = ((positionY % canvasSize) + canvasSize) % canvasSize;

            // Store current brush state
            brushR = r;
            brushG = g;
            brushB = b;
            brushSize = size;
            brushOpacity = opacity;

            // If pen is lifted, just move without painting
            if (!penDown)
            {
                return;
            }

            // Draw a stroke from old position to new position
            float dx = positionX - oldX;
            float dy = positionY - oldY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // Skip very long jumps (wrapping artifacts) - just paint a dot
            if (dist > canvasSize / 2f)
            {
                PaintDab(positionX, positionY, r, g, b, size, opacity, brushType);
                return;
            }

            totalDistance += dist;
            strokeSegments++;

            // Interpolate along the stroke with slight curvature
            int steps = Math.Max(1, (int)(dist / (size * 0.3f)));
            // Perpendicular offset for subtle curve based on pressure
            float curvature = (pressure - 0.5f) * size * 0.8f;
            float perpX = -dy / Math.Max(1f, dist);
            float perpY = dx / Math.Max(1f, dist);

            for (int s = 0; s <= steps; s++)
            {
                float t = steps > 0 ? (float)s / steps : 0f;
                // Quadratic bezier-like offset: max curve at midpoint
                float curveOffset = curvature * 4f * t * (1f - t);
                float px = oldX + dx * t + perpX * curveOffset;
                float py = oldY + dy * t + perpY * curveOffset;
                // Vary opacity along stroke for natural feel (taper at ends)
                float strokeOpacity = opacity * (0.5f + 0.5f * (float)Math.Sin(t * Math.PI));
                // Vary size along stroke slightly (thicker in middle)
                float strokeSize = size * (0.7f + 0.3f * (float)Math.Sin(t * Math.PI));
                PaintDab(px, py, r, g, b, strokeSize, strokeOpacity, brushType);
            }
        }

        private void PaintDab(float cx, float cy, float r, float g, float b,
                              float radius, float opacity, float brushTypeRaw)
        {
            // Select brush type: 0-0.33 soft, 0.33-0.66 hard, 0.66-1.0 splatter
            float bt = Clamp01(Sigmoid(brushTypeRaw));
            int iRadius = (int)Math.Ceiling(radius);
            int minX = Math.Max(0, (int)cx - iRadius);
            int maxX = Math.Min(canvasSize - 1, (int)cx + iRadius);
            int minY = Math.Max(0, (int)cy - iRadius);
            int maxY = Math.Min(canvasSize - 1, (int)cy + iRadius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;
                    float radiusSq = radius * radius;

                    if (distSq > radiusSq) continue;

                    float dist = (float)Math.Sqrt(distSq);
                    float normalizedDist = dist / radius;

                    float alpha;
                    if (bt < 0.33f)
                    {
                        // Soft round brush: Gaussian-like falloff
                        alpha = (float)Math.Exp(-3.0 * normalizedDist * normalizedDist);
                    }
                    else if (bt < 0.66f)
                    {
                        // Hard round brush: sharp edge with slight antialiasing
                        alpha = normalizedDist < 0.85f ? 1f : Math.Max(0f, (1f - normalizedDist) / 0.15f);
                    }
                    else
                    {
                        // Splatter/spray: random-looking pattern using deterministic hash
                        int hash = HashPixel(x, y, stepCount);
                        float threshold = normalizedDist * normalizedDist;
                        alpha = (hash % 100) / 100f > threshold ? 0.7f : 0f;
                    }

                    alpha *= opacity;
                    if (alpha < 0.004f) continue;

                    BlendPixel(x, y, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255), alpha);
                }
            }
        }

        private void BlendPixel(int x, int y, byte r, byte g, byte b, float alpha)
        {
            byte existingA = canvas[x][y][3];
            if (existingA == 0)
            {
                // First paint on this pixel
                canvas[x][y][0] = r;
                canvas[x][y][1] = g;
                canvas[x][y][2] = b;
                canvas[x][y][3] = (byte)Math.Min(255, (int)(alpha * 255));
                pixelsTouched++;
            }
            else
            {
                // Alpha compositing (source over)
                float srcA = alpha;
                float dstA = existingA / 255f;
                float outA = srcA + dstA * (1f - srcA);

                if (outA > 0.001f)
                {
                    canvas[x][y][0] = (byte)((r * srcA + canvas[x][y][0] * dstA * (1f - srcA)) / outA);
                    canvas[x][y][1] = (byte)((g * srcA + canvas[x][y][1] * dstA * (1f - srcA)) / outA);
                    canvas[x][y][2] = (byte)((b * srcA + canvas[x][y][2] * dstA * (1f - srcA)) / outA);
                    canvas[x][y][3] = (byte)Math.Min(255, (int)(outA * 255));
                }
            }
        }

        private static int HashPixel(int x, int y, int seed)
        {
            // Simple deterministic hash for splatter brush consistency
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1103515245;
            return Math.Abs(h);
        }

        private static float Sigmoid(float x)
        {
            return 1f / (1f + (float)Math.Exp(-x));
        }

        private static float Clamp01(float x)
        {
            return x < 0f ? 0f : (x > 1f ? 1f : x);
        }

        public virtual void Restart()
        {
            positionX = canvasSize / 2f;
            positionY = canvasSize / 2f;
            brushR = 0.5f;
            brushG = 0.5f;
            brushB = 0.5f;
            brushSize = 2f;
            brushOpacity = 0.5f;
            stepCount = 0;
            pixelsTouched = 0;
            totalDistance = 0f;
            strokeSegments = 0;

            for (int i = 0; i < canvasSize; i++)
            {
                for (int j = 0; j < canvasSize; j++)
                {
                    canvas[i][j][0] = 0;
                    canvas[i][j][1] = 0;
                    canvas[i][j][2] = 0;
                    canvas[i][j][3] = 0;
                }
            }
        }
    }
}
