#!/usr/bin/env node

// Original final-boss battle loop: 180 BPM, 4/4, 32 bars.
// Built around distorted guitar, cinematic percussion, brass/choir and DJ stutters.

const fs = require("fs");
const path = require("path");

const sampleRate = 44100;
const bpm = 180;
const beatsPerBar = 4;
const bars = 32;
const secondsPerBeat = 60 / bpm;
const secondsPerBar = beatsPerBar * secondsPerBeat;
const totalSeconds = bars * secondsPerBar;
const frameCount = Math.round(totalSeconds * sampleRate);
const usePianoLead = process.argv.includes("--piano");
const explicitOutputPath = process.argv
  .slice(2)
  .find((argument) => !argument.startsWith("--"));
const outputPath = path.resolve(
  explicitOutputPath || (usePianoLead
    ? "Assets/BGM/FinalBoss_PianoOverdrive_180BPM_32Bars_Loop.wav"
    : "Assets/BGM/FinalBoss_Overdrive_180BPM_32Bars_Loop.wav"),
);

const left = new Float32Array(frameCount);
const right = new Float32Array(frameCount);
const roomLeft = new Float32Array(frameCount);
const roomRight = new Float32Array(frameCount);

let randomState = 0xb055180;
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

function guitarOscillator(frequency, time, phase, drive = 2.8) {
  let raw = 0;
  for (let harmonic = 1; harmonic <= 9; harmonic += 1) {
    raw += Math.sin(2 * Math.PI * frequency * harmonic * time + phase * harmonic)
      / harmonic ** 1.08;
  }
  const detuned = Math.sin(2 * Math.PI * frequency * 1.004 * time + phase + 0.43);
  return Math.tanh((raw * 0.42 + detuned * 0.24) * drive);
}

function addGuitarChug(note, bar, step, level, accent = false, pan = 0) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  const duration = accent ? secondsPerBeat * 1.45 : secondsPerBeat * 0.58;
  addCircular(left, right, gridTime(bar, step), duration, (time) => {
    const attack = smoothstep(time / 0.0025);
    const decay = accent
      ? (time < duration * 0.56 ? 1 : 1 - smoothstep((time - duration * 0.56) / (duration * 0.44)))
      : Math.exp(-time * 19);
    const pick = (random() * 2 - 1) * Math.exp(-time * 85);
    const root = guitarOscillator(frequency, time, phase, accent ? 2.4 : 3.2);
    const fifth = guitarOscillator(frequency * 1.5, time, phase * 0.67, 2.4);
    const sample = level * attack * decay * (0.61 * root + 0.31 * fifth + 0.08 * pick);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addGuitarLead(note, bar, step, level, lengthSteps, bend = 0) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.5);
  const duration = lengthSteps * secondsPerBeat * 0.25 + secondsPerBeat * 0.5;
  addCircular(roomLeft, roomRight, gridTime(bar, step), duration, (time) => {
    const attack = smoothstep(time / 0.006);
    const releaseStart = duration * 0.58;
    const release = time < releaseStart ? 1 : 1 - smoothstep((time - releaseStart) / (duration - releaseStart));
    const bendAmount = bend * smoothstep(time / Math.max(0.05, duration * 0.5));
    const bentFrequency = frequency * 2 ** (bendAmount / 12);
    const vibrato = 1 + 0.0035 * Math.sin(2 * Math.PI * 6.4 * time);
    const sample = level * attack * release
      * guitarOscillator(bentFrequency * vibrato, time, phase, 2.15);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addPianoLead(note, bar, step, level, lengthSteps) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.5);
  const duration = lengthSteps * secondsPerBeat * 0.25 + secondsPerBeat * 0.5;
  addCircular(roomLeft, roomRight, gridTime(bar, step), duration, (time) => {
    const hammerAttack = smoothstep(time / 0.0018);
    const releaseStart = duration * 0.62;
    const release = time < releaseStart
      ? 1
      : 1 - smoothstep((time - releaseStart) / (duration - releaseStart));
    const fundamental = Math.sin(2 * Math.PI * frequency * time + phase)
      * Math.exp(-time * 2.7);
    const secondString = Math.sin(2 * Math.PI * frequency * 1.0017 * time + phase + 0.18)
      * Math.exp(-time * 3.0);
    const harmonic2 = Math.sin(2 * Math.PI * frequency * 2.003 * time + phase * 0.43)
      * Math.exp(-time * 4.3);
    const harmonic3 = Math.sin(2 * Math.PI * frequency * 3.011 * time + phase * 0.81)
      * Math.exp(-time * 6.2);
    const harmonic4 = Math.sin(2 * Math.PI * frequency * 4.027 * time + phase * 1.17)
      * Math.exp(-time * 8.5);
    const hammer = (
      Math.sin(2 * Math.PI * 3180 * time + phase)
      + 0.55 * Math.sin(2 * Math.PI * 4670 * time + phase * 0.7)
    ) * Math.exp(-time * 78);
    const sample = level * 1.28 * hammerAttack * release
      * (0.48 * fundamental
        + 0.24 * secondString
        + 0.15 * harmonic2
        + 0.075 * harmonic3
        + 0.035 * harmonic4
        + 0.02 * hammer);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addLowBass(note, bar, step, level, accent) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const duration = accent ? secondsPerBeat * 1.2 : secondsPerBeat * 0.55;
  addCircular(left, right, gridTime(bar, step), duration, (time) => {
    const attack = smoothstep(time / 0.003);
    const decay = accent ? Math.exp(-time * 4.6) : Math.exp(-time * 14);
    const sub = Math.sin(2 * Math.PI * frequency * time + phase);
    const grind = Math.tanh(
      (Math.sin(2 * Math.PI * frequency * 2 * time + phase * 0.5)
        + 0.4 * Math.sin(2 * Math.PI * frequency * 3 * time)) * 2.2,
    );
    const sample = level * attack * decay * (0.76 * sub + 0.24 * grind);
    return [sample * 0.71, sample * 0.71];
  });
}

function addChoir(note, bar, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  const hold = secondsPerBar * 0.82;
  const release = secondsPerBeat * 2.2;
  addCircular(roomLeft, roomRight, bar * secondsPerBar, hold + release, (time) => {
    const attack = smoothstep(time / 0.16);
    const releaseEnvelope = time < hold ? 1 : 1 - smoothstep((time - hold) / release);
    const vibrato = 1 + 0.0018 * Math.sin(2 * Math.PI * 4.7 * time + phase);
    const fundamental = Math.sin(2 * Math.PI * frequency * vibrato * time + phase);
    const formant1 = Math.sin(2 * Math.PI * frequency * 2 * time + phase * 0.4);
    const formant2 = Math.sin(2 * Math.PI * frequency * 3 * time + phase * 1.1);
    const breath = (random() * 2 - 1) * 0.035;
    const sample = level * attack * releaseEnvelope
      * (0.66 * fundamental + 0.21 * formant1 + 0.1 * formant2 + breath);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addBrassHit(notes, bar, step, level) {
  notes.slice(0, 4).forEach((note, index) => {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const [gainLeft, gainRight] = panGains(index / 3 * 1.3 - 0.65);
    addCircular(roomLeft, roomRight, gridTime(bar, step), secondsPerBeat * 1.7, (time) => {
      const attack = smoothstep(time / 0.024);
      const decay = Math.exp(-time * 3.8);
      const scoop = 1 - 0.018 * Math.exp(-time * 18);
      let tone = 0;
      for (let harmonic = 1; harmonic <= 5; harmonic += 1) {
        tone += Math.sin(2 * Math.PI * frequency * scoop * harmonic * time + phase * harmonic)
          / harmonic ** 1.25;
      }
      const sample = level * attack * decay * tone * 0.58;
      return [sample * gainLeft, sample * gainRight];
    });
  });
}

function addKick(bar, step, level = 1) {
  const phase = random() * Math.PI * 2;
  addCircular(left, right, gridTime(bar, step), secondsPerBeat * 1.05, (time) => {
    const attack = smoothstep(time / 0.0018);
    const decay = Math.exp(-time * 15.5);
    const frequency = 44 + 92 * Math.exp(-time * 28);
    const body = Math.sin(2 * Math.PI * frequency * time + phase);
    const clipped = Math.tanh(body * 2.4);
    const click = (random() * 2 - 1) * Math.exp(-time * 95);
    const sample = 0.185 * level * attack * decay * (0.94 * clipped + 0.06 * click);
    return [sample * 0.71, sample * 0.71];
  });
}

function addSnare(bar, step, level = 1, ghost = false) {
  let previousNoise = 0;
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.16);
  addCircular(left, right, gridTime(bar, step), ghost ? 0.2 : 0.52, (time) => {
    const attack = smoothstep(time / 0.0018);
    const raw = random() * 2 - 1;
    const crack = raw - previousNoise * 0.58;
    previousNoise = raw;
    const fast = Math.exp(-time * (ghost ? 31 : 19));
    const room = Math.exp(-time * (ghost ? 24 : 8.2));
    const tone = Math.sin(2 * Math.PI * 148 * time + phase) * Math.exp(-time * 16);
    const amplitude = ghost ? 0.027 : 0.082;
    const sample = amplitude * level * attack
      * (0.6 * crack * fast + 0.26 * raw * room + 0.14 * tone);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addCymbal(bar, step, level = 1, open = false) {
  let previousNoise = 0;
  const [gainLeft, gainRight] = panGains(step % 4 < 2 ? -0.5 : 0.5);
  addCircular(left, right, gridTime(bar, step), open ? 0.55 : 0.075, (time) => {
    const attack = smoothstep(time / 0.0012);
    const raw = random() * 2 - 1;
    const high = raw - previousNoise * 0.96;
    previousNoise = raw;
    const decay = Math.exp(-time * (open ? 8.5 : 57));
    const metal = (
      Math.sin(2 * Math.PI * 6120 * time)
      + Math.sin(2 * Math.PI * 8140 * time + 0.7)
      + Math.sin(2 * Math.PI * 9730 * time + 1.4)
    ) / 3;
    const sample = 0.026 * level * attack * decay * (0.78 * high + 0.22 * metal);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addTaiko(bar, step, note, level, pan) {
  const frequency = midiToHz(note);
  const phase = random() * Math.PI * 2;
  const [gainLeft, gainRight] = panGains(pan);
  addCircular(roomLeft, roomRight, gridTime(bar, step), secondsPerBeat * 1.6, (time) => {
    const attack = smoothstep(time / 0.003);
    const decay = Math.exp(-time * 8.2);
    const swept = frequency * (1 + 0.48 * Math.exp(-time * 25));
    const body = Math.sin(2 * Math.PI * swept * time + phase);
    const skin = (random() * 2 - 1) * Math.exp(-time * 42);
    const sample = 0.105 * level * attack * decay * (0.91 * body + 0.09 * skin);
    return [sample * gainLeft, sample * gainRight];
  });
}

function addRiser(bar) {
  let previousNoise = 0;
  addCircular(roomLeft, roomRight, gridTime(bar, 12), secondsPerBeat, (time) => {
    const progress = time / secondsPerBeat;
    const raw = random() * 2 - 1;
    const high = raw - previousNoise * 0.9;
    previousNoise = raw;
    const tremolo = 0.5 + 0.5 * Math.sin(2 * Math.PI * (7 + progress * 24) * time);
    const sample = 0.022 * progress ** 1.6 * high * tremolo;
    return [sample * 0.72, -sample * 0.72];
  });
}

const chords = {
  Em: { root: 28, notes: [52, 55, 59, 64, 67] },
  Em9: { root: 28, notes: [52, 55, 59, 62, 66] },
  C: { root: 24, notes: [48, 52, 55, 60, 64] },
  Cmaj7: { root: 24, notes: [48, 52, 55, 59, 64] },
  Am: { root: 21, notes: [45, 48, 52, 57, 60] },
  F: { root: 29, notes: [53, 57, 60, 65, 69] },
  D: { root: 26, notes: [50, 54, 57, 62, 66] },
  G: { root: 31, notes: [55, 59, 62, 67, 71] },
  B7: { root: 23, notes: [47, 51, 54, 57, 59] },
};

const progression = [
  "Em9", "Cmaj7", "Am", "B7", "Em", "F", "C", "B7",
  "Em9", "D", "Cmaj7", "B7", "Am", "C", "Em", "B7",
  "Em", "G", "F", "B7", "C", "D", "Em9", "B7",
  "Am", "F", "Cmaj7", "B7", "Em", "F", "B7", "B7",
];

const kickPatterns = [
  [0, 2, 4, 7, 8, 10, 12, 14],
  [0, 3, 6, 8, 9, 12, 15],
  [0, 2, 5, 7, 8, 11, 14],
  [0, 3, 4, 6, 9, 12, 13, 15],
  [0, 2, 4, 7, 10, 12, 14, 15],
  [0, 3, 6, 8, 11, 12, 15],
  [0, 2, 5, 8, 9, 11, 14],
  [0, 3, 4, 7, 10, 13, 15],
];
const chugPatterns = [
  [0, 3, 4, 6, 8, 11, 12, 14],
  [0, 2, 5, 7, 8, 10, 13, 15],
  [0, 3, 6, 8, 9, 12, 14],
  [0, 2, 4, 7, 10, 11, 13, 15],
];
const harmonicMinor = [0, 2, 3, 5, 7, 8, 11, 12];

for (let bar = 0; bar < bars; bar += 1) {
  const chord = chords[progression[bar]];
  const section = Math.floor(bar / 4);
  const energy = [0.84, 0.94, 1.0, 0.91, 1.05, 1.1, 1.03, 1.16][section];

  chord.notes.slice(0, 4).forEach((note, voice) => {
    addChoir(note, bar, 0.012 * energy, voice / 3 * 1.35 - 0.675);
  });

  addGuitarChug(chord.root + 12, bar, 0, 0.052 * energy, true, -0.4);
  addGuitarChug(chord.root + 12, bar, 8, 0.043 * energy, true, 0.4);

  const chugs = chugPatterns[(bar + section) % chugPatterns.length];
  chugs.forEach((step, index) => {
    const note = index % 5 === 4 ? chord.root + 19 : chord.root + 12;
    addGuitarChug(note, bar, step, 0.039 * energy, false, index % 2 === 0 ? -0.58 : 0.58);
    addLowBass(index % 5 === 4 ? chord.root + 7 : chord.root, bar, step, 0.072 * energy, step === 0);
  });

  const kicks = kickPatterns[(bar + section) % kickPatterns.length];
  kicks.forEach((step, index) => addKick(bar, step, index === 0 ? 1 : 0.82));
  addSnare(bar, 4, 0.94);
  addSnare(bar, 12, 1.05);
  if ((bar + section) % 2 === 1) addSnare(bar, 10, 0.88, true);
  if (bar % 4 === 3) addSnare(bar, 15, 1, true);

  for (let step = 0; step < 16; step += 1) {
    if ((step + bar * 3) % 11 === 0 && step !== 0) continue;
    addCymbal(bar, step, step % 4 === 2 ? 1.08 : (step % 2 === 1 ? 0.74 : 0.48));
  }
  if (bar % 2 === 1) addCymbal(bar, 14, 0.9, true);
  if (bar % 4 === 0) addCymbal(bar, 0, 1.15, true);

  if (bar % 2 === 0) {
    addBrassHit(chord.notes, bar, 0, 0.025 * energy);
    addBrassHit(chord.notes, bar, 10, 0.018 * energy);
  } else {
    addBrassHit(chord.notes, bar, 6, 0.021 * energy);
    addBrassHit(chord.notes, bar, 13, 0.017 * energy);
  }

  const leadSteps = section % 2 === 0 ? [1, 5, 9, 11, 14] : [2, 6, 7, 10, 13, 15];
  leadSteps.forEach((step, index) => {
    if ((index + bar + section) % 5 === 4) return;
    const degree = (index * 2 + bar + section) % harmonicMinor.length;
    const leadRoot = 64 + (section >= 4 ? 12 : 0);
    const leadNote = leadRoot + harmonicMinor[degree];
    const leadLength = index % 3 === 0 ? 2 : 1;
    if (usePianoLead) {
      addPianoLead(leadNote, bar, step, 0.028 * energy, leadLength);
    } else {
      addGuitarLead(
        leadNote,
        bar,
        step,
        0.021 * energy,
        leadLength,
        index === leadSteps.length - 1 ? 1 : 0);
    }
  });

  if (bar % 4 === 3) {
    addTaiko(bar, 10, 38, 0.72, -0.5);
    addTaiko(bar, 12, 35, 0.82, -0.12);
    addTaiko(bar, 14, 31, 0.94, 0.3);
    addTaiko(bar, 15, 28, 1.05, 0.55);
    addRiser(bar);
  }
}

// Short circular room taps for drums, brass and choir.
const roomTaps = [
  [0.047, 0.072, -0.3], [0.083, 0.061, 0.38], [0.139, 0.052, -0.45],
  [0.229, 0.043, 0.41], [0.367, 0.034, -0.27], [0.577, 0.026, 0.32],
  [0.887, 0.018, -0.36], [1.237, 0.012, 0.24],
];
for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] += roomLeft[frame] * 0.82;
  right[frame] += roomRight[frame] * 0.82;
}
for (const [delaySeconds, gain, crossfeed] of roomTaps) {
  const delayFrames = Math.round(delaySeconds * sampleRate);
  for (let sourceFrame = 0; sourceFrame < frameCount; sourceFrame += 1) {
    let targetFrame = sourceFrame + delayFrames;
    if (targetFrame >= frameCount) targetFrame -= frameCount;
    const sourceLeft = roomLeft[sourceFrame];
    const sourceRight = roomRight[sourceFrame];
    left[targetFrame] += gain * (sourceLeft + sourceRight * crossfeed);
    right[targetFrame] += gain * (sourceRight - sourceLeft * crossfeed);
  }
}

// DJ-style buffer stutters: the entire mix catches on a tiny slice like a lagging deck.
function applyStutter(bar, startStep, sliceDivision, repeats, gateEvery = 0) {
  const startFrame = Math.round(gridTime(bar, startStep) * sampleRate);
  const sixteenthFrames = Math.round(secondsPerBeat * 0.25 * sampleRate);
  const sliceFrames = Math.max(32, Math.round(sixteenthFrames / sliceDivision));
  const totalFrames = sliceFrames * repeats;
  const sourceLeft = new Float32Array(sliceFrames);
  const sourceRight = new Float32Array(sliceFrames);
  for (let frame = 0; frame < sliceFrames; frame += 1) {
    sourceLeft[frame] = left[startFrame + frame];
    sourceRight[frame] = right[startFrame + frame];
  }
  const edgeFrames = Math.min(Math.round(0.002 * sampleRate), Math.floor(sliceFrames / 5));
  for (let frame = 0; frame < totalFrames && startFrame + frame < frameCount; frame += 1) {
    const repetition = Math.floor(frame / sliceFrames);
    const sourceFrame = frame % sliceFrames;
    let edgeGain = 1;
    if (sourceFrame < edgeFrames) edgeGain = smoothstep(sourceFrame / edgeFrames);
    if (sourceFrame >= sliceFrames - edgeFrames) {
      edgeGain *= smoothstep((sliceFrames - 1 - sourceFrame) / edgeFrames);
    }
    const gated = gateEvery > 0 && repetition % gateEvery === gateEvery - 1;
    const gain = gated ? 0.12 : edgeGain;
    left[startFrame + frame] = sourceLeft[sourceFrame] * gain;
    right[startFrame + frame] = sourceRight[sourceFrame] * gain;
  }
}

applyStutter(7, 12, 1, 4, 0);   // Four 1/16 repeats.
applyStutter(15, 12, 2, 8, 4);  // Eight 1/32 repeats with a gated hiccup.
applyStutter(23, 8, 2, 12, 3);  // Longer escalating deck lock.
applyStutter(30, 12, 4, 16, 4); // 1/64 final-battle glitch burst.

let meanLeft = 0;
let meanRight = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  meanLeft += left[frame];
  meanRight += right[frame];
}
meanLeft /= frameCount;
meanRight /= frameCount;

for (let frame = 0; frame < frameCount; frame += 1) {
  left[frame] = Math.tanh((left[frame] - meanLeft) * 2.0);
  right[frame] = Math.tanh((right[frame] - meanRight) * 2.0);
}

function repairBoundary(channel) {
  const repairFrames = Math.round(0.1 * sampleRate);
  const startFrame = frameCount - repairFrames;
  const desiredLast = channel[0] - (channel[1] - channel[0]);
  const correction = desiredLast - channel[frameCount - 1];
  for (let frame = startFrame; frame < frameCount; frame += 1) {
    const progress = (frame - startFrame) / (repairFrames - 1);
    channel[frame] += correction * smoothstep(progress);
  }
}

repairBoundary(left);
repairBoundary(right);

let peak = 0;
for (let frame = 0; frame < frameCount; frame += 1) {
  peak = Math.max(peak, Math.abs(left[frame]), Math.abs(right[frame]));
}
const targetPeak = 0.87;
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
  leadInstrument: usePianoLead ? "piano" : "distorted guitar",
  bpm,
  timeSignature: "4/4",
  bars,
  durationSeconds: totalSeconds,
  exactFrames: frameCount,
  smallestRhythmicGrid: "1/16 note (DJ effects down to 1/64)",
  stutterBars: [8, 16, 24, 31],
  targetPeak,
}, null, 2));
