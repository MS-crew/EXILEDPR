// -----------------------------------------------------------------------
// <copyright file="PitchShiftFilter.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.Audio.Filters
{
    using System;

    using Exiled.API.Interfaces;

    using UnityEngine;

    /// <summary>
    /// A true DSP Granular Pitch Shifter based on the smbPitchShift algorithm.
    /// </summary>
    public sealed class PitchShiftFilter : IAudioFilter
    {
        private const int MaxFrameLength = 8192;

        private readonly float[] gInFIFO = new float[MaxFrameLength];
        private readonly float[] gOutFIFO = new float[MaxFrameLength];
        private readonly float[] gFFTworksp = new float[2 * MaxFrameLength];
        private readonly float[] gLastPhase = new float[(MaxFrameLength / 2) + 1];
        private readonly float[] gSumPhase = new float[(MaxFrameLength / 2) + 1];
        private readonly float[] gOutputAccum = new float[2 * MaxFrameLength];
        private readonly float[] gAnaFreq = new float[MaxFrameLength];
        private readonly float[] gAnaMagn = new float[MaxFrameLength];
        private readonly float[] gSynFreq = new float[MaxFrameLength];
        private readonly float[] gSynMagn = new float[MaxFrameLength];

        private long gRover = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PitchShiftFilter"/> class.
        /// </summary>
        /// <param name="pitch">The pitch multiplier. Above 1.0 for Helium/Thin voice, below 1.0 for Deep/Monster voice.</param>
        public PitchShiftFilter(float pitch = 1.5f)
        {
            Pitch = pitch;
        }

        /// <summary>
        /// Gets or sets the pitch multiplier dynamically during playback.
        /// </summary>
        public float Pitch
        {
            get => field;
            set => field = Mathf.Clamp(value, 0.1f, 4.0f);
        }

        /// <summary>
        /// Processes the raw PCM audio frame directly before it is encoded and sending.
        /// </summary>
        /// <param name="frame">The array of PCM audio samples.</param>
        public void Process(float[] frame)
        {
            if (Mathf.Abs(Pitch - 1.0f) < 0.001f)
                return;

            SmbPitchShift(Pitch, frame.Length, 2048, 4, VoiceChat.VoiceChatSettings.SampleRate, frame, frame);
        }

        /// <summary>
        /// Stephan M. Bernsee's Phase Vocoder routine.
        /// </summary>
        private void SmbPitchShift(float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp, float sampleRate, float[] indata, float[] outdata)
        {
            double magn, phase, tmp, window, real, imag;
            double freqPerBin, expct;
            long i, k, qpd, index, inFifoLatency, stepSize, fftFrameSize2;

            fftFrameSize2 = fftFrameSize / 2;
            stepSize = fftFrameSize / osamp;
            freqPerBin = sampleRate / (double)fftFrameSize;
            expct = 2.0 * Math.PI * (double)stepSize / (double)fftFrameSize;
            inFifoLatency = fftFrameSize - stepSize;

            if (gRover == 0)
                gRover = inFifoLatency;

            for (i = 0; i < numSampsToProcess; i++)
            {
                gInFIFO[gRover] = indata[i];
                outdata[i] = gOutFIFO[gRover - inFifoLatency];
                gRover++;

                if (gRover >= fftFrameSize)
                {
                    gRover = inFifoLatency;

                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = (-0.5 * Math.Cos(2.0 * Math.PI * k / (double)fftFrameSize)) + 0.5;
                        gFFTworksp[2 * k] = (float)(gInFIFO[k] * window);
                        gFFTworksp[(2 * k) + 1] = 0.0f;
                    }

                    SmbFft(gFFTworksp, fftFrameSize, -1);

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        real = gFFTworksp[2 * k];
                        imag = gFFTworksp[(2 * k) + 1];

                        magn = 2.0 * Math.Sqrt((real * real) + (imag * imag));
                        phase = Math.Atan2(imag, real);

                        tmp = phase - gLastPhase[k];
                        gLastPhase[k] = (float)phase;

                        tmp -= (double)k * expct;
                        qpd = (long)(tmp / Math.PI);
                        if (qpd >= 0)
                            qpd += qpd & 1;
                        else
                            qpd -= qpd & 1;
                        tmp -= Math.PI * (double)qpd;

                        tmp = osamp * tmp / (2.0 * Math.PI);
                        tmp = ((double)k * freqPerBin) + (tmp * freqPerBin);

                        gAnaMagn[k] = (float)magn;
                        gAnaFreq[k] = (float)tmp;
                    }

                    for (int zero = 0; zero < fftFrameSize; zero++)
                    {
                        gSynMagn[zero] = 0;
                        gSynFreq[zero] = 0;
                    }

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        index = (long)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            gSynMagn[index] += gAnaMagn[k];
                            gSynFreq[index] = gAnaFreq[k] * pitchShift;
                        }
                    }

                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        magn = gSynMagn[k];
                        tmp = gSynFreq[k];

                        tmp -= (double)k * freqPerBin;
                        tmp /= freqPerBin;
                        tmp = 2.0 * Math.PI * tmp / osamp;
                        tmp += (double)k * expct;

                        gSumPhase[k] += (float)tmp;
                        phase = gSumPhase[k];

                        gFFTworksp[2 * k] = (float)(magn * Math.Cos(phase));
                        gFFTworksp[(2 * k) + 1] = (float)(magn * Math.Sin(phase));
                    }

                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++)
                        gFFTworksp[k] = 0.0f;

                    SmbFft(gFFTworksp, fftFrameSize, 1);

                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = (-0.5 * Math.Cos(2.0 * Math.PI * (double)k / (double)fftFrameSize)) + 0.5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[2 * k] / (fftFrameSize2 * osamp));
                    }

                    for (k = 0; k < stepSize; k++)
                        gOutFIFO[k] = gOutputAccum[k];

                    Array.Copy(gOutputAccum, stepSize, gOutputAccum, 0, fftFrameSize);
                    for (k = 0; k < inFifoLatency; k++)
                        gInFIFO[k] = gInFIFO[k + stepSize];
                }
            }
        }

        private void SmbFft(float[] fftBuffer, long fftFrameSize, long sign)
        {
            float wr, wi, arg, temp;
            float tr, ti, ur, ui;
            long i, bitm, j, le, le2, k;

            for (i = 2; i < (2 * fftFrameSize) - 2; i += 2)
            {
                for (bitm = 2, j = 0; bitm < 2 * fftFrameSize; bitm <<= 1)
                {
                    if ((i & bitm) != 0)
                        j++;

                    j <<= 1;
                }

                if (i < j)
                {
                    temp = fftBuffer[i];
                    fftBuffer[i] = fftBuffer[j];
                    fftBuffer[j] = temp;
                    temp = fftBuffer[i + 1];
                    fftBuffer[i + 1] = fftBuffer[j + 1];
                    fftBuffer[j + 1] = temp;
                }
            }

            long max = (long)((Math.Log(fftFrameSize) / Math.Log(2.0)) + 0.5);
            for (k = 0, le = 2; k < max; k++)
            {
                le <<= 1;
                le2 = le >> 1;
                ur = 1.0f;
                ui = 0.0f;
                arg = (float)(Math.PI / (le2 >> 1));
                wr = (float)Math.Cos(arg);
                wi = (float)(sign * Math.Sin(arg));
                for (j = 0; j < le2; j += 2)
                {
                    for (i = j; i < 2 * fftFrameSize; i += le)
                    {
                        tr = (fftBuffer[i + le2] * ur) - (fftBuffer[i + le2 + 1] * ui);
                        ti = (fftBuffer[i + le2] * ui) + (fftBuffer[i + le2 + 1] * ur);
                        fftBuffer[i + le2] = fftBuffer[i] - tr;
                        fftBuffer[i + le2 + 1] = fftBuffer[i + 1] - ti;
                        fftBuffer[i] += tr;
                        fftBuffer[i + 1] += ti;
                    }

                    tr = (ur * wr) - (ui * wi);
                    ui = (ur * wi) + (ui * wr);
                    ur = tr;
                }
            }
        }
    }
}