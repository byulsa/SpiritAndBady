#!/usr/bin/env node

// Deterministic procedural soundtrack generator.
// Produces a seamless 32-bar, 4/4 loop at 60 BPM as stereo PCM16 WAV.

const fs = require("fs");
const path = require("path");

const sampleRate = 44100;
const bpm = 60;
const beatsPerBar = 4;
const bars = 32;
const secondsPerBeat = 60 / bpm;
const totalSeconds = bars * beatsPerBar * secondsPerBeat;
const frameCount = Math.round(totalSeconds * sampleRate);
const outputPath = path.resolve(
  process.argv[2] || "Assets/BGM/Dreamy_60BPM_32Bars_Loop.wav",
);

const left = new Float32Array(frameCount);
const right = new Float32Array(frameCount);
const ambienceLeft = new Float32Array(frameCount);
const ambienceRight = new Float32Array(frameCount);

let randomState = 0x51f15e;
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

function addCircular(targetLeft, targetRight, startSeconds, durationSeconds, render) {
  const startFrame = Math.round(startSeconds * sampleRate);
  const renderFrames = Math.round(durationSeconds * sampleRate);
  for (let localFrame = 0; localFrame < renderFrames; localFrame += 1) {
    let targetFrame = (startFrame + localFrame) % frameCount;
    if (targetFrame < 0) targetFrame += frameCount;
    const [sampleLeft, sampleRight] = render(localFrame / sampleRate, localFrame, renderFrames);
    targetLeft[targetFrame] += sampleLeft;
    targetRight[targetFrame] += sampleRight;
  }
}

function addPadNote(note, bar, level, pan, brightness) {
  const frequency = midiToHz(note);
  const startSeconds = bar * beatsPerBar * secondsPerBeat;
  const holdSeconds = beatsPerBar * secondsPerBeat * 0.92;
  const attackSeconds = 1.35;
  const releaseSeconds = 3.2;
  const totalDuration = holdSeconds + releaseSeconds;
  const detune = (random() - 0.5) * 0.0022;
  const phase = random() * Math.PI * 2;
  const tremoloRate = 0.045 + random() * 0.035;
  const [gainLeft, gainRight] = panGains(pan);

  addCircular(ambienceLeft, ambienceRight, startSeconds, totalDuration, (time) => {
    const attack = smoothstep(time / attackSeconds);
    const release = time <= holdSeconds
      ? 1
      : 1 - smoothstep((time - holdSeconds) / releaseSeconds);
    const envelope = attack * release;
    const drift = 1 + 0.0011 * Math.sin(2 * Math.PI * 0.072 * time + phase * 0.37);
    const fundamental = Math.sin(2 * Math.PI * frequency * drift * time + phase);
    const detuned = Math.sin(2 * Math.PI * frequency * (1 + detune) * time + phase + 0.8);
    const octave = Math.sin(2 * Math.PI * frequency * 2 * time + phase * 1.7);
    const fifth = Math.sin(2 * Math.PI * frequency * 1.5 * time + phase * 0.6);
    const tremolo = 0.91 + 0.09 * Math.sin(2 * Math.PI * tremoloRate * time + phase);
    const sample = level * envelope * tremolo
      * (0.56 * fundamental + 0.30 * detuned + brightness * 0.10 * octave + 0.04 * fifth);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addBass(note, bar, level) {
  const frequency = midiToHz(note);
  const startSeconds = bar * beatsPerBar;
  const totalDuration = 5.2;
  const phase = random() * Math.PI * 2;

  addCircular(left, right, startSeconds, totalDuration, (time) => {
    const attack = smoothstep(time / 0.32);
    const release = time < 3.5 ? 1 : 1 - smoothstep((time - 3.5) / 1.7);
    const envelope = attack * release;
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const warmth = Math.sin(2 * Math.PI * frequency * 2 * time + phase * 0.7);
    const sample = level * envelope * (0.88 * body + 0.12 * warmth);
    return [sample * 0.71, sample * 0.71];
  });
}

function addBell(note, startBeat, level, pan, longTail = false) {
  const frequency = midiToHz(note);
  const startSeconds = startBeat * secondsPerBeat;
  const duration = longTail ? 6.5 : 4.2;
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);

  addCircular(ambienceLeft, ambienceRight, startSeconds, duration, (time) => {
    const attack = smoothstep(time / 0.012);
    const decay = Math.exp(-time * (longTail ? 0.63 : 0.92));
    const shimmerDecay = Math.exp(-time * 1.45);
    const fundamental = Math.sin(2 * Math.PI * frequency * time + phase);
    const partial2 = Math.sin(2 * Math.PI * frequency * 2.006 * time + phase * 0.3);
    const partial3 = Math.sin(2 * Math.PI * frequency * 3.87 * time + phase * 1.2);
    const sample = level * attack
      * (0.72 * decay * fundamental + 0.20 * shimmerDecay * partial2 + 0.08 * shimmerDecay * partial3);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addPluck(note, startBeat, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);

  addCircular(ambienceLeft, ambienceRight, startBeat, 2.15, (time) => {
    const attack = smoothstep(time / 0.018);
    const decay = Math.exp(-time * 2.35);
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const glass = Math.sin(2 * Math.PI * frequency * 2.01 * time + phase * 0.4);
    const sample = level * attack * decay * (0.76 * body + 0.24 * glass);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addPulse(beat, accent) {
  const startSeconds = beat * secondsPerBeat;
  const level = accent ? 0.075 : 0.034;
  const seed = random() * Math.PI * 2;

  addCircular(left, right, startSeconds, 0.7, (time) => {
    const envelope = Math.exp(-time * (accent ? 7.2 : 9.5));
    const frequency = 52 + 24 * Math.exp(-time * 18);
    const tone = Math.sin(2 * Math.PI * frequency * time + seed);
    const breath = (random() * 2 - 1) * Math.exp(-time * 18);
    const sample = level * envelope * (0.92 * tone + 0.08 * breath);
    return [sample * 0.71, sample * 0.71];
  });
}

const progression = [
  { name: "Dmaj9", bass: 38, notes: [50, 57, 61, 64, 66] },
  { name: "Aadd9/C#", bass: 37, notes: [49, 57, 59, 64, 69] },
  { name: "Bm7", bass: 35, notes: [47, 54, 57, 62, 66] },
  { name: "Gmaj7", bass: 31, notes: [43, 50, 54, 59, 66] },
  { name: "D/F#", bass: 42, notes: [50, 57, 61, 64, 69] },
  { name: "Em9", bass: 40, notes: [52, 55, 59, 62, 66] },
  { name: "Gmaj9", bass: 31, notes: [43, 50, 54, 57, 59] },
  { name: "A7sus4", bass: 33, notes: [45, 52, 55, 59, 62] },

  { name: "Bm9", bass: 35, notes: [47, 54, 57, 61, 62] },
  { name: "F#m7", bass: 30, notes: [42, 49, 52, 57, 61] },
  { name: "Gmaj7", bass: 31, notes: [43, 50, 54, 59, 62] },
  { name: "D/A", bass: 33, notes: [45, 50, 54, 57, 61] },
  { name: "Em9", bass: 40, notes: [52, 55, 59, 62, 66] },
  { name: "Bm7", bass: 35, notes: [47, 54, 57, 62, 66] },
  { name: "Gmaj9", bass: 31, notes: [43, 50, 54, 57, 59] },
  { name: "A6sus", bass: 33, notes: [45, 52, 54, 59, 62] },

  { name: "Dmaj9", bass: 38, notes: [50, 57, 61, 64, 66] },
  { name: "A/C#", bass: 37, notes: [49, 52, 57, 61, 64] },
  { name: "Bm11", bass: 35, notes: [47, 52, 54, 57, 62] },
  { name: "F#m/A", bass: 33, notes: [45, 49, 54, 57, 61] },
  { name: "Gmaj7", bass: 31, notes: [43, 50, 54, 59, 66] },
  { name: "D/F#", bass: 30, notes: [42, 50, 57, 61, 64] },
  { name: "Em9", bass: 28, notes: [40, 47, 54, 55, 59] },
  { name: "A7sus4", bass: 33, notes: [45, 52, 55, 59, 62] },

  { name: "Bm11", bass: 35, notes: [47, 52, 54, 57, 62] },
  { name: "A/C#", bass: 37, notes: [49, 52, 57, 61, 64] },
  { name: "Dmaj9", bass: 38, notes: [50, 57, 61, 64, 66] },
  { name: "Gmaj7", bass: 31, notes: [43, 50, 54, 59, 62] },
  { name: "Em9", bass: 28, notes: [40, 47, 54, 55, 59] },
  { name: "F#m7", bass: 30, notes: [42, 49, 52, 57, 61] },
  { name: "Gmaj9", bass: 31, notes: [43, 50, 54, 57, 59] },
  { name: "Aadd9", bass: 33, notes: [45, 52, 59, 61, 64] },
];

progression.forEach((chord, bar) => {
  const section = Math.floor(bar / 8);
  const padLevel = [0.035, 0.039, 0.042, 0.036][section];
  const width = [0.58, 0.72, 0.82, 0.64][section];
  chord.notes.forEach((note, voice) => {
    const alternatingPan = (voice / (chord.notes.length - 1) * 2 - 1) * width;
    addPadNote(note, bar, padLevel, alternatingPan, section >= 2 ? 1.1 : 0.85);
  });
  addBass(chord.bass, bar, section === 2 ? 0.066 : 0.058);
});

// A restrained melody: enough identity to evolve across 32 bars without crowding gameplay.
const melody = [
  [2, 66], [5.5, 69], [9, 71], [14, 69], [18, 66], [22.5, 64], [27, 66], [30.5, 64],
  [33, 69], [36.5, 71], [41, 74], [46, 73], [50, 71], [53.5, 69], [58, 66], [62.5, 64],
  [65, 73], [68, 74], [70.5, 76], [73.5, 78], [77, 76], [81, 74], [85.5, 73], [89, 71], [94, 69],
  [97, 66], [101.5, 69], [105, 73], [110, 71], [113, 69], [117.5, 66], [122, 64], [126, 61],
];
melody.forEach(([beat, note], index) => {
  addBell(note, beat, index >= 16 && index <= 24 ? 0.058 : 0.046, index % 2 === 0 ? -0.38 : 0.38, index % 5 === 0);
});

// Arpeggios bloom in the middle and recede before the loop returns to bar one.
for (let bar = 8; bar < 28; bar += 1) {
  const chord = progression[bar];
  const fadeIn = Math.min(1, (bar - 7) / 4);
  const fadeOut = Math.min(1, (28 - bar) / 5);
  const level = 0.018 * fadeIn * fadeOut;
  for (let step = 0; step < 8; step += 1) {
    const pattern = [0, 2, 1, 3, 2, 4, 3, 1];
    const note = chord.notes[pattern[step] % chord.notes.length] + 12;
    const beat = bar * beatsPerBar + step * 0.5;
    addPluck(note, beat, level, step % 2 === 0 ? -0.52 : 0.52);
  }
}

for (let beat = 0; beat < bars * beatsPerBar; beat += 1) {
  addPulse(beat, beat % beatsPerBar === 0);
}

// A circular multi-tap ambience bus makes the reverb itself loop seamlessly.
const reverbTaps = [
  [0.137, 0.105, -0.35], [0.223, 0.091, 0.42], [0.347, 0.083, -0.58],
  [0.521, 0.074, 0.51], [0.743, 0.065, -0.26], [1.019, 0.057, 0.31],
  [1.397, 0.047, -0.46], [1.891, 0.039, 0.55], [2.531, 0.031, -0.18],
  [3.277, 0.024, 0.23], [4.153, 0.017, -0.39],
];

for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] += ambienceLeft[frame] * 0.78;
  right[frame] += ambienceRight[frame] * 0.78;
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

// Remove DC, apply a gentle safety saturator, and normalize to a BGM-friendly peak.
let meanLeft = 0;
let meanRight = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  meanLeft += left[frame];
  meanRight += right[frame];
}
meanLeft /= frameCount;
meanRight /= frameCount;

let peak = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] = Math.tanh((left[frame] - meanLeft) * 1.18);
  right[frame] = Math.tanh((right[frame] - meanRight) * 1.18);
}

// Match both value and first derivative across the final boundary. The smooth
// quarter-second correction has zero slope at each end, so it cannot add a click.
function repairLoopBoundary(channel) {
  const repairFrames = Math.round(0.25 * sampleRate);
  const startFrame = frameCount - repairFrames;
  const firstDelta = channel[1] - channel[0];
  const desiredLast = channel[0] - firstDelta;
  const correction = desiredLast - channel[frameCount - 1];
  for (let frame = startFrame; frame < frameCount; frame += 1) {
    const progress = (frame - startFrame) / (repairFrames - 1);
    channel[frame] += correction * smoothstep(progress);
  }
}

repairLoopBoundary(left);
repairLoopBoundary(right);

for (let frame = 0; frame < frameCount; frame += 1) {
  peak = Math.max(peak, Math.abs(left[frame]), Math.abs(right[frame]));
}
// Leave roughly 5 dB of headroom so judgement SFX can sit clearly above the music.
const targetPeak = 0.56;
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

const seamJumpLeft = Math.abs(left[0] - left[frameCount - 1]) * normalization;
const seamJumpRight = Math.abs(right[0] - right[frameCount - 1]) * normalization;
console.log(JSON.stringify({
  output: outputPath,
  bpm,
  timeSignature: "4/4",
  bars,
  durationSeconds: totalSeconds,
  sampleRate,
  channels: 2,
  peak: targetPeak,
  seamJumpLeft,
  seamJumpRight,
}, null, 2));
