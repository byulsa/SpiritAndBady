#!/usr/bin/env node

// Deterministic 100 BPM rhythm-game soundtrack generator.
// 4/4, 32 bars, stereo PCM16, seamless circular ambience, no leading padding.

const fs = require("fs");
const path = require("path");

const sampleRate = 44100;
const bpm = 100;
const beatsPerBar = 4;
const bars = 32;
const secondsPerBeat = 60 / bpm;
const secondsPerBar = beatsPerBar * secondsPerBeat;
const totalSeconds = bars * secondsPerBar;
const frameCount = Math.round(totalSeconds * sampleRate);
const outputPath = path.resolve(
  process.argv[2] || "Assets/BGM/SynthRhythm_100BPM_32Bars_Loop.wav",
);

const left = new Float32Array(frameCount);
const right = new Float32Array(frameCount);
const ambienceLeft = new Float32Array(frameCount);
const ambienceRight = new Float32Array(frameCount);

let randomState = 0x100b9a7;
function random() {
  randomState ^= randomState << 13;
  randomState ^= randomState >>> 17;
  randomState ^= randomState << 5;
  return (randomState >>> 0) / 4294967296;
}

function midiToHz(note) {
  return 440 * 2 ** ((note - 69) / 12);
}

function smoothstep(value) {
  const x = Math.max(0, Math.min(1, value));
  return x * x * (3 - 2 * x);
}

function panGains(pan) {
  const angle = (Math.max(-1, Math.min(1, pan)) + 1) * Math.PI * 0.25;
  return [Math.cos(angle), Math.sin(angle)];
}

function gridTime(bar, sixteenth) {
  return bar * secondsPerBar + sixteenth * secondsPerBeat * 0.25;
}

function addCircular(targetLeft, targetRight, startSeconds, durationSeconds, render) {
  const startFrame = Math.round(startSeconds * sampleRate);
  const renderFrames = Math.round(durationSeconds * sampleRate);
  for (let localFrame = 0; localFrame < renderFrames; localFrame += 1) {
    let targetFrame = (startFrame + localFrame) % frameCount;
    if (targetFrame < 0) targetFrame += frameCount;
    const [sampleLeft, sampleRight] = render(localFrame / sampleRate, localFrame);
    targetLeft[targetFrame] += sampleLeft;
    targetRight[targetFrame] += sampleRight;
  }
}

function additiveSaw(frequency, time, phase, harmonicDecay = 1) {
  let value = 0;
  for (let harmonic = 1; harmonic <= 7; harmonic += 1) {
    value += Math.sin(2 * Math.PI * frequency * harmonic * time + phase * harmonic)
      / harmonic ** harmonicDecay;
  }
  return value * 0.52;
}

function addPad(note, bar, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const detune = 1 + (random() - 0.5) * 0.004;
  const [gainLeft, gainRight] = panGains(pan);
  const hold = secondsPerBar * 0.88;
  const release = 1.35;

  addCircular(ambienceLeft, ambienceRight, bar * secondsPerBar, hold + release, (time) => {
    const attackEnvelope = smoothstep(time / 0.16);
    const releaseEnvelope = time < hold ? 1 : 1 - smoothstep((time - hold) / release);
    const envelope = attackEnvelope * releaseEnvelope;
    const wobble = 1 + 0.0018 * Math.sin(2 * Math.PI * 0.31 * time + phase);
    const warm = additiveSaw(frequency * wobble, time, phase, 1.18);
    const wide = additiveSaw(frequency * detune, time, phase + 0.7, 1.28);
    const sample = level * envelope * (0.62 * warm + 0.38 * wide);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addSynthBass(note, bar, step, level, accent = false) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  addCircular(left, right, gridTime(bar, step), accent ? 0.52 : 0.38, (time) => {
    const attack = smoothstep(time / 0.005);
    const decay = Math.exp(-time * (accent ? 4.7 : 6.5));
    const pitchDrop = frequency * (1 + 0.035 * Math.exp(-time * 25));
    const sub = Math.sin(2 * Math.PI * pitchDrop * time + phase);
    const edge = additiveSaw(frequency * 2, time, phase * 0.6, 1.35);
    const sample = level * attack * decay * (0.78 * sub + 0.22 * edge);
    return [sample * 0.71, sample * 0.71];
  });
}

function addArp(note, bar, step, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), 0.42, (time) => {
    const attack = smoothstep(time / 0.004);
    const decay = Math.exp(-time * 8.2);
    const body = additiveSaw(frequency, time, phase, 1.35);
    const glass = Math.sin(2 * Math.PI * frequency * 2.004 * time + phase * 0.4);
    const sample = level * attack * decay * (0.78 * body + 0.22 * glass);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addChordStab(notes, bar, step, level) {
  notes.slice(1, 5).forEach((note, index) => {
    const frequency = midiToHz(note + 12);
    const phase = random() * Math.PI * 2;
    const pan = index / 3 * 1.2 - 0.6;
    const [gainLeft, gainRight] = panGains(pan);
    addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), 0.58, (time) => {
      const attack = smoothstep(time / 0.008);
      const decay = Math.exp(-time * 5.4);
      const sample = level * attack * decay * additiveSaw(frequency, time, phase, 1.4);
      return [sample * gainLeft, sample * gainRight];
    });
  });
}

function addLead(note, bar, step, level, lengthSteps = 2) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const pan = (random() - 0.5) * 0.5;
  const [gainLeft, gainRight] = panGains(pan);
  const duration = Math.max(0.3, lengthSteps * secondsPerBeat * 0.25 + 0.25);
  addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), duration, (time) => {
    const attack = smoothstep(time / 0.018);
    const releaseStart = duration * 0.56;
    const release = time < releaseStart ? 1 : 1 - smoothstep((time - releaseStart) / (duration - releaseStart));
    const vibrato = 1 + 0.0028 * Math.sin(2 * Math.PI * 5.2 * time);
    const main = Math.sin(2 * Math.PI * frequency * vibrato * time + phase);
    const edge = Math.sin(2 * Math.PI * frequency * 2 * time + phase * 0.4);
    const sample = level * attack * release * (0.79 * main + 0.21 * edge);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addKick(bar, step, level = 1) {
  const phase = random() * Math.PI * 2;
  addCircular(left, right, gridTime(bar, step), 0.5, (time) => {
    const attack = smoothstep(time / 0.0035);
    const envelope = attack * Math.exp(-time * 9.2);
    const frequency = 47 + 72 * Math.exp(-time * 20);
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const click = (random() * 2 - 1) * Math.exp(-time * 65);
    const sample = 0.16 * level * envelope * (0.95 * body + 0.05 * click);
    return [sample * 0.71, sample * 0.71];
  });
}

function addSnare(bar, step, level = 1, ghost = false) {
  let previousNoise = 0;
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.16);
  addCircular(left, right, gridTime(bar, step), ghost ? 0.28 : 0.72, (time) => {
    const attack = smoothstep(time / 0.003);
    const rawNoise = random() * 2 - 1;
    const crack = rawNoise - previousNoise * 0.68;
    previousNoise = rawNoise;
    const shortDecay = Math.exp(-time * (ghost ? 22 : 13));
    const roomDecay = Math.exp(-time * (ghost ? 18 : 6.2));
    const tone = Math.sin(2 * Math.PI * 186 * time + phase) * Math.exp(-time * 12);
    const amplitude = ghost ? 0.028 : 0.078;
    const sample = amplitude * level * attack
      * (0.58 * crack * shortDecay + 0.28 * rawNoise * roomDecay + 0.14 * tone);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addHat(bar, step, level = 1, open = false) {
  let previousNoise = 0;
  const [gainLeft, gainRight] = panGains(step % 4 < 2 ? -0.46 : 0.46);
  addCircular(left, right, gridTime(bar, step), open ? 0.42 : 0.11, (time) => {
    const attack = smoothstep(time / 0.0018);
    const rawNoise = random() * 2 - 1;
    const bright = rawNoise - previousNoise * 0.94;
    previousNoise = rawNoise;
    const decay = Math.exp(-time * (open ? 10 : 38));
    const metal = Math.sin(2 * Math.PI * 7810 * time) * 0.12;
    const sample = 0.024 * level * attack * decay * (0.88 * bright + metal);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addTom(bar, step, note, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  addCircular(left, right, gridTime(bar, step), 0.58, (time) => {
    const attack = smoothstep(time / 0.004);
    const decay = Math.exp(-time * 7);
    const sweptFrequency = frequency * (1 + 0.34 * Math.exp(-time * 20));
    const sample = 0.073 * level * attack * decay
      * Math.sin(2 * Math.PI * sweptFrequency * time + phase);
    return [sample * gainLeft, sample * gainRight];
  });
}

const progression = [
  { name: "F#m9", root: 30, notes: [54, 57, 61, 64, 68] },
  { name: "Dmaj7", root: 26, notes: [50, 54, 57, 61, 66] },
  { name: "Aadd9", root: 33, notes: [57, 59, 61, 64, 69] },
  { name: "E6", root: 28, notes: [52, 56, 59, 61, 64] },

  { name: "F#m", root: 30, notes: [54, 57, 61, 66, 69] },
  { name: "Dmaj9", root: 26, notes: [50, 54, 57, 61, 64] },
  { name: "Eadd9", root: 28, notes: [52, 56, 59, 61, 66] },
  { name: "C#m7", root: 25, notes: [49, 52, 56, 59, 64] },

  { name: "Bm9", root: 23, notes: [47, 50, 54, 57, 61] },
  { name: "Dmaj7", root: 26, notes: [50, 54, 57, 61, 66] },
  { name: "A", root: 33, notes: [57, 61, 64, 69, 73] },
  { name: "Eadd9", root: 28, notes: [52, 56, 59, 61, 66] },

  { name: "F#m9", root: 30, notes: [54, 57, 61, 64, 68] },
  { name: "E/G#", root: 32, notes: [56, 59, 64, 68, 71] },
  { name: "Dmaj7", root: 26, notes: [50, 54, 57, 61, 66] },
  { name: "C#sus4", root: 25, notes: [49, 54, 56, 61, 66] },

  { name: "F#m", root: 30, notes: [54, 57, 61, 66, 69] },
  { name: "Amaj7", root: 33, notes: [57, 61, 64, 68, 73] },
  { name: "E", root: 28, notes: [52, 56, 59, 64, 68] },
  { name: "Dmaj9", root: 26, notes: [50, 54, 57, 61, 64] },

  { name: "Bm7", root: 23, notes: [47, 50, 54, 57, 59] },
  { name: "D", root: 26, notes: [50, 54, 57, 62, 66] },
  { name: "F#m9", root: 30, notes: [54, 57, 61, 64, 68] },
  { name: "E6", root: 28, notes: [52, 56, 59, 61, 64] },

  { name: "Dmaj7", root: 26, notes: [50, 54, 57, 61, 66] },
  { name: "Eadd9", root: 28, notes: [52, 56, 59, 61, 66] },
  { name: "C#m7", root: 25, notes: [49, 52, 56, 59, 64] },
  { name: "F#m9", root: 30, notes: [54, 57, 61, 64, 68] },

  { name: "Bm9", root: 23, notes: [47, 50, 54, 57, 61] },
  { name: "Dmaj7", root: 26, notes: [50, 54, 57, 61, 66] },
  { name: "E7sus4", root: 28, notes: [52, 57, 59, 62, 66] },
  { name: "C#7", root: 25, notes: [49, 53, 56, 59, 61] },
];

const kickPatterns = [
  [0, 6, 8, 11], [0, 5, 8, 14], [0, 3, 8, 10, 15], [0, 7, 10, 13],
  [0, 6, 9, 12], [0, 5, 8, 11, 15], [0, 3, 7, 10, 14], [0, 6, 8, 13],
];
const bassPatterns = [
  [0, 3, 6, 8, 11, 14],
  [0, 2, 5, 8, 10, 13, 15],
  [0, 3, 7, 8, 11, 14],
  [0, 5, 7, 10, 12, 15],
];
const stabPatterns = [[3, 10], [6, 11, 15], [2, 7, 14], [5, 10, 13]];

progression.forEach((chord, bar) => {
  const wave = Math.floor(bar / 4);
  const waveEnergy = [0.78, 0.88, 0.94, 0.86, 1.0, 1.04, 0.96, 1.08][wave];

  chord.notes.forEach((note, voice) => {
    const pan = voice / (chord.notes.length - 1) * 1.5 - 0.75;
    addPad(note, bar, 0.018 * waveEnergy, pan);
  });

  const bassPattern = bassPatterns[(wave + bar) % bassPatterns.length];
  bassPattern.forEach((step, index) => {
    const note = index % 4 === 3 ? chord.root + 7 : chord.root;
    addSynthBass(note, bar, step, 0.068 * waveEnergy, step === 0);
  });

  const kicks = kickPatterns[(bar + wave) % kickPatterns.length];
  kicks.forEach((step, index) => addKick(bar, step, index === 0 ? 1 : 0.82));
  addSnare(bar, 4, 0.9);
  addSnare(bar, 12, 1.0);
  if ((bar + wave) % 3 === 1) addSnare(bar, 10, 0.9, true);
  if ((bar + wave) % 4 === 3) addSnare(bar, 15, 1.0, true);

  for (let step = 0; step < 16; step += 1) {
    const deliberatelyMissing = (step + bar * 3) % 11 === 0 && step !== 0;
    if (!deliberatelyMissing) {
      const offbeatAccent = step % 4 === 2;
      addHat(bar, step, offbeatAccent ? 1.05 : (step % 2 === 0 ? 0.48 : 0.72));
    }
  }
  if (bar % 2 === 1) addHat(bar, 14, 0.82, true);

  stabPatterns[(wave + bar) % stabPatterns.length]
    .forEach((step) => addChordStab(chord.notes, bar, step, 0.012 * waveEnergy));

  const arpMask = wave % 2 === 0
    ? [1, 2, 5, 7, 9, 10, 13, 15]
    : [1, 3, 4, 7, 9, 11, 12, 15];
  arpMask.forEach((step, index) => {
    const arpIndex = [0, 2, 1, 3, 2, 4, 1, 3][index];
    const note = chord.notes[arpIndex % chord.notes.length] + 12;
    addArp(note, bar, step, 0.014 * waveEnergy, index % 2 === 0 ? -0.54 : 0.54);
  });

  if (bar % 4 === 3) {
    addTom(bar, 11, 43, 0.68, -0.38);
    addTom(bar, 13, 40, 0.78, 0.02);
    addTom(bar, 15, 36, 0.9, 0.4);
  }
});

// Eight related four-bar motifs: one recognizable phrase per gameplay wave.
const leadRoots = [66, 69, 71, 73, 74, 76, 73, 68];
const leadOffsets = [0, 3, 7, 10, 14];
const leadIntervals = [0, 3, 5, 2, 7];
for (let wave = 0; wave < 8; wave += 1) {
  for (let barInWave = 0; barInWave < 4; barInWave += 1) {
    const bar = wave * 4 + barInWave;
    leadOffsets.forEach((step, index) => {
      if ((index + barInWave + wave) % 4 === 3) return;
      const scaleShift = leadIntervals[(index + barInWave) % leadIntervals.length];
      const octaveDrop = barInWave === 3 && index > 2 ? -12 : 0;
      addLead(leadRoots[wave] + scaleShift + octaveDrop, bar, step, 0.027, index % 2 === 0 ? 2 : 1);
    });
  }
}

// Circular multi-tap ambience: delay tails wrap into bar one instead of being cut.
const reverbTaps = [
  [0.089, 0.082, -0.31], [0.137, 0.071, 0.38], [0.211, 0.064, -0.49],
  [0.337, 0.056, 0.43], [0.487, 0.048, -0.27], [0.731, 0.040, 0.35],
  [1.013, 0.032, -0.41], [1.409, 0.025, 0.29], [1.933, 0.018, -0.22],
];

for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] += ambienceLeft[frame] * 0.83;
  right[frame] += ambienceRight[frame] * 0.83;
}
for (const [delaySeconds, gain, crossfeed] of reverbTaps) {
  const delayFrames = Math.round(delaySeconds * sampleRate);
  for (let sourceFrame = 0; sourceFrame < frameCount; sourceFrame += 1) {
    let targetFrame = sourceFrame + delayFrames;
    if (targetFrame >= frameCount) targetFrame -= frameCount;
    const sourceLeft = ambienceLeft[sourceFrame];
    const sourceRight = ambienceRight[sourceFrame];
    left[targetFrame] += gain * (sourceLeft + sourceRight * crossfeed);
    right[targetFrame] += gain * (sourceRight - sourceLeft * crossfeed);
  }
}

let meanLeft = 0;
let meanRight = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  meanLeft += left[frame];
  meanRight += right[frame];
}
meanLeft /= frameCount;
meanRight /= frameCount;

for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] = Math.tanh((left[frame] - meanLeft) * 1.12);
  right[frame] = Math.tanh((right[frame] - meanRight) * 1.12);
}

function repairLoopBoundary(channel) {
  const repairFrames = Math.round(0.16 * sampleRate);
  const startFrame = frameCount - repairFrames;
  const desiredLast = channel[0] - (channel[1] - channel[0]);
  const correction = desiredLast - channel[frameCount - 1];
  for (let frame = startFrame; frame < frameCount; frame += 1) {
    const progress = (frame - startFrame) / (repairFrames - 1);
    channel[frame] += correction * smoothstep(progress);
  }
}

repairLoopBoundary(left);
repairLoopBoundary(right);

let peak = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  peak = Math.max(peak, Math.abs(left[frame]), Math.abs(right[frame]));
}
const targetPeak = 0.76;
const normalization = targetPeak / peak;

const wav = Buffer.allocUnsafe(44 + frameCount * 4);
wav.write("RIFF", 0);
wav.writeUInt32LE(36 + frameCount * 4, 4);
wav.write("WAVE", 8);
wav.write("fmt ", 12);
wav.writeUInt32LE(16, 16);
wav.writeUInt16LE(1, 20);
wav.writeUInt16LE(2, 22);
wav.writeUInt32LE(sampleRate, 24);
wav.writeUInt32LE(sampleRate * 4, 28);
wav.writeUInt16LE(4, 32);
wav.writeUInt16LE(16, 34);
wav.write("data", 36);
wav.writeUInt32LE(frameCount * 4, 40);

for (let frame = 0; frame < frameCount; frame += 1) {
  const ditherLeft = (random() - random()) / 65536;
  const ditherRight = (random() - random()) / 65536;
  const sampleLeft = Math.max(-1, Math.min(1, left[frame] * normalization + ditherLeft));
  const sampleRight = Math.max(-1, Math.min(1, right[frame] * normalization + ditherRight));
  wav.writeInt16LE(Math.round(sampleLeft * 32767), 44 + frame * 4);
  wav.writeInt16LE(Math.round(sampleRight * 32767), 46 + frame * 4);
}

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, wav);

console.log(JSON.stringify({
  output: outputPath,
  bpm,
  timeSignature: "4/4",
  bars,
  wavesAtFourBarsEach: bars / 4,
  durationSeconds: totalSeconds,
  sampleRate,
  channels: 2,
  smallestRhythmicGrid: "1/16 note",
  targetPeak,
}, null, 2));
