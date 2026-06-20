#!/usr/bin/env node

// Adds an eighth-note-limited dark drum arrangement to the dreamy 60 BPM loop.
// Every musical trigger lands on the 1/8-note grid; ambience tails remain free.

const fs = require("fs");
const path = require("path");

const sourcePath = path.resolve(
  process.argv[2] || "Assets/BGM/Dreamy_60BPM_32Bars_Loop.wav",
);
const outputPath = path.resolve(
  process.argv[3] || "Assets/BGM/Dreamy_DarkRhythm_60BPM_32Bars_Loop.wav",
);

const source = fs.readFileSync(sourcePath);
if (source.toString("ascii", 0, 4) !== "RIFF" || source.toString("ascii", 8, 12) !== "WAVE") {
  throw new Error("Source must be a RIFF/WAVE file.");
}

let offset = 12;
let format;
let dataOffset;
let dataSize;
while (offset + 8 <= source.length) {
  const chunkId = source.toString("ascii", offset, offset + 4);
  const chunkSize = source.readUInt32LE(offset + 4);
  if (chunkId === "fmt ") {
    format = {
      audioFormat: source.readUInt16LE(offset + 8),
      channels: source.readUInt16LE(offset + 10),
      sampleRate: source.readUInt32LE(offset + 12),
      bitsPerSample: source.readUInt16LE(offset + 22),
    };
  } else if (chunkId === "data") {
    dataOffset = offset + 8;
    dataSize = chunkSize;
  }
  offset += 8 + chunkSize + (chunkSize % 2);
}

if (!format || dataOffset === undefined) throw new Error("Missing WAV format or data chunk.");
if (format.audioFormat !== 1 || format.channels !== 2 || format.bitsPerSample !== 16) {
  throw new Error("Expected stereo PCM16 source audio.");
}

const sampleRate = format.sampleRate;
const bpm = 60;
const secondsPerBeat = 60 / bpm;
const bars = 32;
const beatsPerBar = 4;
const eighthsPerBar = 8;
const frameCount = dataSize / 4;
const expectedFrames = sampleRate * bars * beatsPerBar * secondsPerBeat;
if (frameCount !== expectedFrames) {
  throw new Error(`Expected ${expectedFrames} frames, got ${frameCount}.`);
}

const dryLeft = new Float32Array(frameCount);
const dryRight = new Float32Array(frameCount);
for (let frame = 0; frame < frameCount; frame += 1) {
  dryLeft[frame] = source.readInt16LE(dataOffset + frame * 4) / 32768;
  dryRight[frame] = source.readInt16LE(dataOffset + frame * 4 + 2) / 32768;
}

// A subtle low-frequency tilt keeps the dreaminess but nudges the palette darker.
const left = new Float32Array(frameCount);
const right = new Float32Array(frameCount);
const lowpassAlpha = 1 - Math.exp(-2 * Math.PI * 720 / sampleRate);
let lowLeft = 0;
let lowRight = 0;
for (let warmup = 0; warmup < 2; warmup += 1) {
  for (let frame = 0; frame < frameCount; frame += 1) {
    lowLeft += lowpassAlpha * (dryLeft[frame] - lowLeft);
    lowRight += lowpassAlpha * (dryRight[frame] - lowRight);
  }
}
for (let frame = 0; frame < frameCount; frame += 1) {
  lowLeft += lowpassAlpha * (dryLeft[frame] - lowLeft);
  lowRight += lowpassAlpha * (dryRight[frame] - lowRight);
  left[frame] = dryLeft[frame] * 0.69 + lowLeft * 0.19;
  right[frame] = dryRight[frame] * 0.69 + lowRight * 0.19;
}

let randomState = 0xd4a4c0de;
function random() {
  randomState ^= randomState << 13;
  randomState ^= randomState >>> 17;
  randomState ^= randomState << 5;
  return (randomState >>> 0) / 4294967296;
}

function smoothstep(value) {
  const x = Math.max(0, Math.min(1, value));
  return x * x * (3 - 2 * x);
}

function midiToHz(note) {
  return 440 * 2 ** ((note - 69) / 12);
}

function panGains(pan) {
  const angle = (Math.max(-1, Math.min(1, pan)) + 1) * Math.PI * 0.25;
  return [Math.cos(angle), Math.sin(angle)];
}

function addCircular(startSeconds, durationSeconds, render) {
  const startFrame = Math.round(startSeconds * sampleRate);
  const renderFrames = Math.round(durationSeconds * sampleRate);
  for (let localFrame = 0; localFrame < renderFrames; localFrame += 1) {
    let targetFrame = (startFrame + localFrame) % frameCount;
    if (targetFrame < 0) targetFrame += frameCount;
    const [sampleLeft, sampleRight] = render(localFrame / sampleRate, localFrame);
    left[targetFrame] += sampleLeft;
    right[targetFrame] += sampleRight;
  }
}

function gridTime(bar, eighth) {
  return bar * beatsPerBar * secondsPerBeat + eighth * 0.5 * secondsPerBeat;
}

function addKick(bar, eighth, level = 1) {
  const phase = random() * Math.PI * 2;
  addCircular(gridTime(bar, eighth), 0.62, (time) => {
    const attack = smoothstep(time / 0.006);
    const envelope = attack * Math.exp(-time * 7.5);
    const frequency = 46 + 58 * Math.exp(-time * 16);
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const sub = Math.sin(2 * Math.PI * 43 * time + phase * 0.7);
    const sample = 0.145 * level * envelope * (0.82 * body + 0.18 * sub);
    return [sample * 0.71, sample * 0.71];
  });
}

function addSnare(bar, eighth, level = 1, ghost = false) {
  let lastNoise = 0;
  const tonePhase = random() * Math.PI * 2;
  const pan = (random() - 0.5) * 0.16;
  const [gainLeft, gainRight] = panGains(pan);
  addCircular(gridTime(bar, eighth), ghost ? 0.38 : 0.82, (time) => {
    const attack = smoothstep(time / 0.004);
    const rawNoise = random() * 2 - 1;
    const brightNoise = rawNoise - lastNoise * 0.72;
    lastNoise = rawNoise;
    const crack = Math.exp(-time * (ghost ? 18 : 12));
    const room = Math.exp(-time * (ghost ? 14 : 5.4));
    const tone = Math.sin(2 * Math.PI * 164 * time + tonePhase) * Math.exp(-time * 10);
    const amplitude = ghost ? 0.030 : 0.070;
    const sample = amplitude * level * attack
      * (0.60 * brightNoise * crack + 0.26 * rawNoise * room + 0.14 * tone);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addHat(bar, eighth, level = 1, open = false) {
  let previousNoise = 0;
  const pan = eighth % 2 === 0 ? -0.42 : 0.42;
  const [gainLeft, gainRight] = panGains(pan);
  const duration = open ? 0.48 : 0.14;
  addCircular(gridTime(bar, eighth), duration, (time) => {
    const attack = smoothstep(time / 0.0025);
    const rawNoise = random() * 2 - 1;
    const metallicNoise = rawNoise - previousNoise * 0.92;
    previousNoise = rawNoise;
    const decay = Math.exp(-time * (open ? 8.5 : 30));
    const shimmer = Math.sin(2 * Math.PI * 6730 * time) * 0.13;
    const sample = 0.027 * level * attack * decay * (0.87 * metallicNoise + shimmer);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addTom(bar, eighth, pitch, level = 1, pan = 0) {
  const baseFrequency = midiToHz(pitch);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  addCircular(gridTime(bar, eighth), 0.78, (time) => {
    const attack = smoothstep(time / 0.005);
    const envelope = attack * Math.exp(-time * 6.2);
    const frequency = baseFrequency * (1 + 0.42 * Math.exp(-time * 18));
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const skin = (random() * 2 - 1) * Math.exp(-time * 30);
    const sample = 0.075 * level * envelope * (0.91 * body + 0.09 * skin);
    return [sample * gainLeft, sample * gainRight];
  });
}

// Root notes mirror the original harmony, one octave down for a shadowy foundation.
const bassRoots = [
  38, 37, 35, 31, 42, 40, 31, 33,
  35, 30, 31, 33, 40, 35, 31, 33,
  38, 37, 35, 33, 31, 30, 28, 33,
  35, 37, 38, 31, 28, 30, 31, 33,
];

function addDarkSub(bar, note) {
  const frequency = midiToHz(note - 12);
  const phase = random() * Math.PI * 2;
  addCircular(bar * beatsPerBar, 5.0, (time) => {
    const attack = smoothstep(time / 0.45);
    const release = time < 3.35 ? 1 : 1 - smoothstep((time - 3.35) / 1.65);
    const pulse = 0.92 + 0.08 * Math.sin(2 * Math.PI * 0.5 * time);
    const sample = 0.018 * attack * release * pulse
      * (0.86 * Math.sin(2 * Math.PI * frequency * time + phase)
        + 0.14 * Math.sin(2 * Math.PI * frequency * 2 * time + phase * 0.7));
    return [sample * 0.71, sample * 0.71];
  });
}

const kickPatterns = [
  [[0, 4], [0, 4], [0, 3, 4], [0, 4, 7]],
  [[0, 3, 4, 7], [0, 4, 5], [0, 3, 6], [0, 4, 7]],
  [[0, 3, 4, 7], [0, 4, 5, 7], [0, 3, 6], [0, 4, 7]],
  [[0, 4], [0, 4, 7], [0, 3, 4, 7], [0, 4]],
];

for (let bar = 0; bar < bars; bar += 1) {
  const section = Math.floor(bar / 8);
  const pattern = kickPatterns[section][bar % 4];
  pattern.forEach((eighth, index) => addKick(bar, eighth, index === 0 ? 1.0 : 0.78));

  addSnare(bar, 2, 0.88);
  addSnare(bar, 6, 1.0);
  if ([5, 11, 18, 22, 29].includes(bar)) addSnare(bar, 5, 1, true);

  const denseHats = section === 1 || section === 2 || (section === 3 && bar >= 28 && bar < 31);
  const hatSteps = denseHats ? [0, 1, 2, 3, 4, 5, 6, 7] : [1, 3, 5, 7];
  hatSteps.forEach((eighth) => {
    const offbeat = eighth % 2 === 1;
    addHat(bar, eighth, offbeat ? 1.0 : 0.48, false);
  });
  if ([7, 15, 23].includes(bar)) addHat(bar, 7, 0.82, true);

  if ([7, 15, 23, 31].includes(bar)) {
    addTom(bar, 5, 43, 0.62, -0.32);
    addTom(bar, 6, 40, 0.72, 0.08);
    addTom(bar, 7, 36, 0.86, 0.34);
  }

  addDarkSub(bar, bassRoots[bar]);
}

// A small boundary correction ensures the first downbeat repeats without a digital click.
function repairLoopBoundary(channel) {
  const repairFrames = Math.round(0.18 * sampleRate);
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
  left[frame] = Math.tanh(left[frame] * 1.06);
  right[frame] = Math.tanh(right[frame] * 1.06);
  peak = Math.max(peak, Math.abs(left[frame]), Math.abs(right[frame]));
}
const targetPeak = 0.60;
const gain = targetPeak / peak;

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
  const sampleLeft = Math.max(-1, Math.min(1, left[frame] * gain + ditherLeft));
  const sampleRight = Math.max(-1, Math.min(1, right[frame] * gain + ditherRight));
  wav.writeInt16LE(Math.round(sampleLeft * 32767), 44 + frame * 4);
  wav.writeInt16LE(Math.round(sampleRight * 32767), 46 + frame * 4);
}

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, wav);

console.log(JSON.stringify({
  source: sourcePath,
  output: outputPath,
  bpm,
  timeSignature: "4/4",
  bars,
  maximumSubdivision: "1/8 note",
  durationSeconds: frameCount / sampleRate,
  sampleRate,
  channels: 2,
  targetPeak,
}, null, 2));
