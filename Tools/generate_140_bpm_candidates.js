#!/usr/bin/env node

// Generates three original, seamless 140 BPM rhythm-game loops.
// All musical triggers use a 1/16-note grid; ambience tails wrap circularly.

const fs = require("fs");
const path = require("path");

const sampleRate = 44100;
const bpm = 140;
const beatsPerBar = 4;
const bars = 32;
const secondsPerBeat = 60 / bpm;
const secondsPerBar = beatsPerBar * secondsPerBeat;
const totalSeconds = bars * secondsPerBar;
const frameCount = Math.round(totalSeconds * sampleRate);

const chordBanks = {
  neon: {
    Am9: { root: 33, notes: [57, 60, 64, 67, 71] },
    Am: { root: 33, notes: [57, 60, 64, 69, 72] },
    Fmaj7: { root: 29, notes: [53, 57, 60, 64, 69] },
    F: { root: 29, notes: [53, 57, 60, 65, 69] },
    Cadd9: { root: 36, notes: [60, 62, 64, 67, 72] },
    C: { root: 36, notes: [60, 64, 67, 72, 76] },
    G6: { root: 31, notes: [55, 59, 62, 64, 67] },
    G: { root: 31, notes: [55, 59, 62, 67, 71] },
    GB: { root: 35, notes: [59, 62, 67, 71, 74] },
    Dm9: { root: 26, notes: [50, 53, 57, 60, 64] },
    Dm: { root: 26, notes: [50, 53, 57, 62, 65] },
    E7: { root: 28, notes: [52, 56, 59, 62, 64] },
  },
  hex: {
    Fsm: { root: 30, notes: [54, 57, 61, 66, 69] },
    Fsm9: { root: 30, notes: [54, 57, 61, 64, 68] },
    D: { root: 26, notes: [50, 54, 57, 62, 66] },
    Dmaj7: { root: 26, notes: [50, 54, 57, 61, 66] },
    A: { root: 33, notes: [57, 61, 64, 69, 73] },
    Aadd9: { root: 33, notes: [57, 59, 61, 64, 69] },
    E: { root: 28, notes: [52, 56, 59, 64, 68] },
    E7: { root: 28, notes: [52, 56, 59, 62, 64] },
    Bm: { root: 23, notes: [47, 50, 54, 59, 62] },
    Bm9: { root: 23, notes: [47, 50, 54, 57, 61] },
    Csm: { root: 25, notes: [49, 52, 56, 61, 64] },
    Cs7: { root: 25, notes: [49, 53, 56, 59, 61] },
  },
  iron: {
    Dm: { root: 26, notes: [50, 53, 57, 62, 65] },
    Dm9: { root: 26, notes: [50, 53, 57, 60, 64] },
    Bb: { root: 22, notes: [46, 50, 53, 58, 62] },
    Bbmaj7: { root: 22, notes: [46, 50, 53, 57, 62] },
    F: { root: 29, notes: [53, 57, 60, 65, 69] },
    C: { root: 24, notes: [48, 52, 55, 60, 64] },
    Gm: { root: 31, notes: [55, 58, 62, 67, 70] },
    Gm9: { root: 31, notes: [55, 58, 62, 65, 69] },
    A7: { root: 33, notes: [57, 61, 64, 67, 69] },
    Edim: { root: 28, notes: [52, 55, 58, 61, 64] },
  },
};

const variants = [
  {
    slug: "NeonVelocity",
    title: "Neon Velocity",
    seed: 0x140a11,
    bank: "neon",
    oscillator: "saw",
    scale: [0, 2, 3, 5, 7, 8, 10, 12],
    leadRoots: [69, 72, 74, 76, 77, 79, 76, 71],
    progression: [
      "Am9", "Fmaj7", "Cadd9", "G6", "Am", "G", "Fmaj7", "E7",
      "Dm9", "F", "C", "G", "Am9", "C", "G6", "Fmaj7",
      "Dm", "Am", "F", "E7", "Am", "Fmaj7", "Dm9", "G",
      "Cadd9", "GB", "Am9", "F", "Dm9", "Fmaj7", "E7", "E7",
    ],
    kickPatterns: [
      [0, 4, 8, 12], [0, 4, 7, 8, 12, 14], [0, 3, 8, 12], [0, 4, 8, 11, 14],
      [0, 4, 6, 8, 12], [0, 4, 8, 10, 15], [0, 3, 7, 8, 12], [0, 4, 8, 13],
    ],
    bassPatterns: [[0, 4, 7, 8, 12, 14], [0, 3, 6, 8, 11, 14], [0, 4, 6, 10, 12, 15]],
    arpMask: [1, 3, 5, 7, 9, 11, 13, 15],
    padLevel: 0.018,
    bassLevel: 0.065,
    arpLevel: 0.014,
    leadLevel: 0.029,
    drumColor: "clean",
    ambience: 0.86,
    targetPeak: 0.87,
  },
  {
    slug: "HexPulse",
    title: "Hex Pulse",
    seed: 0x140b22,
    bank: "hex",
    oscillator: "square",
    scale: [0, 2, 3, 5, 7, 9, 10, 12],
    leadRoots: [66, 69, 71, 73, 78, 76, 74, 68],
    progression: [
      "Fsm9", "Dmaj7", "Aadd9", "E", "Fsm", "E", "D", "Cs7",
      "Bm9", "D", "A", "E7", "Fsm9", "D", "E", "Csm",
      "Fsm", "Aadd9", "E", "Dmaj7", "Bm", "D", "Fsm9", "E7",
      "D", "E", "Csm", "Fsm", "Bm9", "Dmaj7", "E7", "Cs7",
    ],
    kickPatterns: [
      [0, 3, 6, 8, 11, 14], [0, 5, 7, 10, 13], [0, 2, 6, 9, 12, 15], [0, 3, 7, 8, 11, 14],
      [0, 5, 8, 10, 13, 15], [0, 3, 6, 8, 12], [0, 2, 5, 9, 11, 14], [0, 3, 6, 10, 13],
    ],
    bassPatterns: [[0, 3, 6, 8, 11, 14], [0, 2, 5, 8, 10, 13, 15], [0, 3, 7, 9, 12, 14]],
    arpMask: [0, 1, 3, 4, 6, 7, 8, 10, 11, 13, 14, 15],
    padLevel: 0.012,
    bassLevel: 0.058,
    arpLevel: 0.018,
    leadLevel: 0.031,
    drumColor: "digital",
    ambience: 0.69,
    targetPeak: 0.8,
  },
  {
    slug: "IronCircuit",
    title: "Iron Circuit",
    seed: 0x140c33,
    bank: "iron",
    oscillator: "fm",
    scale: [0, 2, 3, 5, 7, 8, 11, 12],
    leadRoots: [62, 65, 67, 69, 74, 72, 70, 69],
    progression: [
      "Dm9", "Bbmaj7", "F", "C", "Dm", "C", "Bb", "A7",
      "Gm9", "Bb", "Dm", "A7", "Dm9", "F", "C", "Bbmaj7",
      "Gm", "Dm", "Bb", "A7", "Dm", "Bbmaj7", "Gm9", "C",
      "F", "C", "Dm9", "Bb", "Gm", "Edim", "A7", "A7",
    ],
    kickPatterns: [
      [0, 3, 7, 10], [0, 6, 8, 11, 15], [0, 2, 7, 9, 14], [0, 5, 8, 10, 13],
      [0, 3, 6, 11, 14], [0, 7, 8, 12, 15], [0, 2, 5, 10, 13], [0, 6, 9, 11, 15],
    ],
    bassPatterns: [[0, 3, 7, 10, 14], [0, 5, 8, 11, 15], [0, 2, 6, 9, 13]],
    arpMask: [1, 2, 5, 7, 9, 10, 13, 15],
    padLevel: 0.014,
    bassLevel: 0.074,
    arpLevel: 0.012,
    leadLevel: 0.025,
    drumColor: "industrial",
    ambience: 0.74,
    targetPeak: 0.8,
  },
];

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

function renderVariant(variant) {
  const left = new Float32Array(frameCount);
  const right = new Float32Array(frameCount);
  const ambienceLeft = new Float32Array(frameCount);
  const ambienceRight = new Float32Array(frameCount);
  let randomState = variant.seed;

  function random() {
    randomState ^= randomState << 13;
    randomState ^= randomState >>> 17;
    randomState ^= randomState << 5;
    return (randomState >>> 0) / 4294967296;
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

  function synthTone(frequency, time, phase, kind = variant.oscillator) {
    let value = 0;
    if (kind === "square") {
      for (let harmonic = 1; harmonic <= 9; harmonic += 2) {
        value += Math.sin(2 * Math.PI * frequency * harmonic * time + phase * harmonic) / harmonic;
      }
      return Math.round(value * 18) / 18 * 0.78;
    }
    if (kind === "fm") {
      const modulator = Math.sin(2 * Math.PI * frequency * 2.01 * time + phase * 0.4);
      return 0.76 * Math.sin(2 * Math.PI * frequency * time + phase + 2.1 * modulator)
        + 0.24 * Math.sin(2 * Math.PI * frequency * 0.5 * time + phase);
    }
    for (let harmonic = 1; harmonic <= 7; harmonic += 1) {
      value += Math.sin(2 * Math.PI * frequency * harmonic * time + phase * harmonic)
        / harmonic ** 1.18;
    }
    return value * 0.5;
  }

  function addPad(note, bar, level, pan) {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const detune = 1 + (random() - 0.5) * 0.0045;
    const [gainLeft, gainRight] = panGains(pan);
    const hold = secondsPerBar * 0.86;
    const release = secondsPerBeat * 2.5;
    addCircular(ambienceLeft, ambienceRight, bar * secondsPerBar, hold + release, (time) => {
      const attack = smoothstep(time / (variant.oscillator === "square" ? 0.035 : 0.1));
      const releaseEnvelope = time < hold ? 1 : 1 - smoothstep((time - hold) / release);
      const wobble = 1 + 0.0015 * Math.sin(2 * Math.PI * 0.43 * time + phase);
      const main = synthTone(frequency * wobble, time, phase);
      const wide = synthTone(frequency * detune, time, phase + 0.63);
      const sample = level * attack * releaseEnvelope * (0.62 * main + 0.38 * wide);
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addBass(note, bar, step, level, accent) {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const duration = accent ? secondsPerBeat * 0.9 : secondsPerBeat * 0.62;
    addCircular(left, right, gridTime(bar, step), duration, (time) => {
      const attack = smoothstep(time / 0.0035);
      const decay = Math.exp(-time * (accent ? 6.2 : 9.2));
      const pitch = frequency * (1 + 0.03 * Math.exp(-time * 28));
      const sub = Math.sin(2 * Math.PI * pitch * time + phase);
      const edge = synthTone(frequency * 2, time, phase * 0.7);
      const sample = level * attack * decay * (0.79 * sub + 0.21 * edge);
      return [sample * 0.71, sample * 0.71];
    });
  }

  function addArp(note, bar, step, level, pan) {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const [gainLeft, gainRight] = panGains(pan);
    addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), secondsPerBeat * 0.62, (time) => {
      const attack = smoothstep(time / 0.0025);
      const decay = Math.exp(-time * (variant.oscillator === "square" ? 14 : 11));
      const body = synthTone(frequency, time, phase);
      const octave = Math.sin(2 * Math.PI * frequency * 2.003 * time + phase * 0.3);
      const sample = level * attack * decay * (0.8 * body + 0.2 * octave);
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addLead(note, bar, step, level, lengthSteps) {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.55);
    const duration = lengthSteps * secondsPerBeat * 0.25 + secondsPerBeat * 0.35;
    addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), duration, (time) => {
      const attack = smoothstep(time / 0.007);
      const releaseStart = duration * 0.52;
      const release = time < releaseStart ? 1 : 1 - smoothstep((time - releaseStart) / (duration - releaseStart));
      const vibrato = 1 + 0.0026 * Math.sin(2 * Math.PI * 5.7 * time);
      const main = synthTone(frequency * vibrato, time, phase);
      const sample = level * attack * release * main;
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addStab(notes, bar, step, level) {
    notes.slice(1, 5).forEach((note, index) => {
      const frequency = midiToHz(note + 12);
      const phase = random() * Math.PI * 2;
      const [gainLeft, gainRight] = panGains(index / 3 * 1.2 - 0.6);
      addCircular(ambienceLeft, ambienceRight, gridTime(bar, step), secondsPerBeat * 0.8, (time) => {
        const envelope = smoothstep(time / 0.004) * Math.exp(-time * 10.5);
        const sample = level * envelope * synthTone(frequency, time, phase);
        return [sample * gainLeft, sample * gainRight];
      });
    });
  }

  function addKick(bar, step, level) {
    const phase = random() * Math.PI * 2;
    const industrial = variant.drumColor === "industrial";
    addCircular(left, right, gridTime(bar, step), secondsPerBeat * 0.86, (time) => {
      const attack = smoothstep(time / 0.0025);
      const decay = Math.exp(-time * (industrial ? 11 : 13));
      const frequency = (industrial ? 43 : 49) + 80 * Math.exp(-time * 24);
      const body = Math.sin(2 * Math.PI * frequency * time + phase);
      const click = (random() * 2 - 1) * Math.exp(-time * 72);
      const distortion = industrial ? Math.tanh(body * 2.4) : body;
      const sample = 0.17 * level * attack * decay * (0.94 * distortion + 0.06 * click);
      return [sample * 0.71, sample * 0.71];
    });
  }

  function addSnare(bar, step, level, ghost = false) {
    let previousNoise = 0;
    const phase = random() * Math.PI * 2;
    const [gainLeft, gainRight] = panGains((random() - 0.5) * 0.22);
    const industrial = variant.drumColor === "industrial";
    addCircular(left, right, gridTime(bar, step), ghost ? 0.22 : 0.58, (time) => {
      const attack = smoothstep(time / 0.002);
      const raw = random() * 2 - 1;
      const crack = raw - previousNoise * (industrial ? 0.55 : 0.75);
      previousNoise = raw;
      const fast = Math.exp(-time * (ghost ? 27 : 17));
      const room = Math.exp(-time * (ghost ? 20 : 7.5));
      const tone = Math.sin(2 * Math.PI * (industrial ? 132 : 196) * time + phase) * Math.exp(-time * 15);
      const amplitude = ghost ? 0.026 : 0.076;
      const sample = amplitude * level * attack * (0.58 * crack * fast + 0.28 * raw * room + 0.14 * tone);
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addHat(bar, step, level, open = false) {
    let previousNoise = 0;
    const [gainLeft, gainRight] = panGains(step % 4 < 2 ? -0.48 : 0.48);
    addCircular(left, right, gridTime(bar, step), open ? 0.34 : 0.085, (time) => {
      const attack = smoothstep(time / 0.0014);
      const raw = random() * 2 - 1;
      const bright = raw - previousNoise * 0.95;
      previousNoise = raw;
      const decay = Math.exp(-time * (open ? 14 : 48));
      const digital = variant.drumColor === "digital"
        ? Math.round(bright * 7) / 7
        : bright;
      const metal = Math.sin(2 * Math.PI * (variant.drumColor === "industrial" ? 5240 : 8230) * time) * 0.14;
      const sample = 0.023 * level * attack * decay * (0.86 * digital + metal);
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addTom(bar, step, note, level, pan) {
    const frequency = midiToHz(note);
    const phase = random() * Math.PI * 2;
    const [gainLeft, gainRight] = panGains(pan);
    addCircular(left, right, gridTime(bar, step), 0.45, (time) => {
      const envelope = smoothstep(time / 0.003) * Math.exp(-time * 9.5);
      const swept = frequency * (1 + 0.4 * Math.exp(-time * 24));
      const body = Math.sin(2 * Math.PI * swept * time + phase);
      const sample = 0.072 * level * envelope * (variant.drumColor === "industrial" ? Math.tanh(body * 2) : body);
      return [sample * gainLeft, sample * gainRight];
    });
  }

  function addTransitionFx(bar) {
    let previousNoise = 0;
    const start = gridTime(bar, 12);
    const duration = secondsPerBeat;
    addCircular(ambienceLeft, ambienceRight, start, duration, (time) => {
      const progress = time / duration;
      const raw = random() * 2 - 1;
      const high = raw - previousNoise * 0.9;
      previousNoise = raw;
      const pulse = 0.55 + 0.45 * Math.sin(2 * Math.PI * (6 + progress * 18) * time);
      const sample = 0.015 * progress ** 1.7 * high * pulse;
      return [sample * 0.75, -sample * 0.75];
    });
  }

  const bank = chordBanks[variant.bank];
  const stabPatterns = [[3, 10], [6, 11, 15], [2, 7, 14], [5, 9, 13]];

  for (let bar = 0; bar < bars; bar += 1) {
    const chord = bank[variant.progression[bar]];
    const wave = Math.floor(bar / 4);
    const energy = [0.82, 0.9, 0.96, 0.88, 1.02, 1.08, 0.98, 1.12][wave];

    chord.notes.forEach((note, voice) => {
      addPad(note, bar, variant.padLevel * energy, voice / 4 * 1.5 - 0.75);
    });

    const bassPattern = variant.bassPatterns[(bar + wave) % variant.bassPatterns.length];
    bassPattern.forEach((step, index) => {
      const bassNote = index % 4 === 3 ? chord.root + 7 : chord.root;
      addBass(bassNote, bar, step, variant.bassLevel * energy, step === 0);
    });

    const kicks = variant.kickPatterns[(bar + wave) % variant.kickPatterns.length];
    kicks.forEach((step, index) => addKick(bar, step, index === 0 ? 1 : 0.82));
    addSnare(bar, 4, 0.92);
    addSnare(bar, 12, 1);
    if ((bar + wave) % 3 === 1) addSnare(bar, 10, 0.9, true);
    if ((bar + wave) % 4 === 3) addSnare(bar, 15, 1, true);

    for (let step = 0; step < 16; step += 1) {
      if ((step + bar * 5 + wave) % 13 === 0 && step !== 0) continue;
      const offbeatAccent = step % 4 === 2;
      const sixteenthAccent = step % 2 === 1;
      addHat(bar, step, offbeatAccent ? 1.08 : (sixteenthAccent ? 0.76 : 0.48));
    }
    if (bar % 2 === 1) addHat(bar, 14, 0.82, true);

    variant.arpMask.forEach((step, index) => {
      const arpIndices = variant.oscillator === "square"
        ? [0, 2, 4, 1, 3, 2, 4, 0, 3, 1, 4, 2]
        : [0, 2, 1, 3, 2, 4, 1, 3, 0, 4, 2, 3];
      const note = chord.notes[arpIndices[index % arpIndices.length] % chord.notes.length] + 12;
      addArp(note, bar, step, variant.arpLevel * energy, index % 2 === 0 ? -0.56 : 0.56);
    });

    stabPatterns[(bar + wave) % stabPatterns.length]
      .forEach((step) => addStab(chord.notes, bar, step, 0.011 * energy));

    if (bar % 4 === 3) {
      addTom(bar, 11, variant.drumColor === "industrial" ? 39 : 43, 0.68, -0.38);
      addTom(bar, 13, variant.drumColor === "industrial" ? 36 : 40, 0.78, 0.02);
      addTom(bar, 15, variant.drumColor === "industrial" ? 33 : 36, 0.9, 0.4);
      addTransitionFx(bar);
    }

    const motifSteps = variant.oscillator === "square"
      ? [1, 4, 6, 9, 11, 14]
      : variant.oscillator === "fm"
        ? [2, 5, 7, 10, 13, 15]
        : [1, 4, 7, 10, 14];
    motifSteps.forEach((step, index) => {
      if ((index + bar + wave) % 5 === 4) return;
      const degree = (index * 2 + bar + wave) % variant.scale.length;
      const octave = index === motifSteps.length - 1 && bar % 4 === 3 ? -12 : 0;
      addLead(
        variant.leadRoots[wave] + variant.scale[degree] + octave,
        bar,
        step,
        variant.leadLevel * energy,
        index % 3 === 0 ? 2 : 1);
    });
  }

  const reverbTaps = [
    [0.071, 0.078, -0.31], [0.113, 0.067, 0.37], [0.179, 0.058, -0.46],
    [0.293, 0.049, 0.42], [0.443, 0.039, -0.28], [0.671, 0.03, 0.33],
    [0.941, 0.022, -0.39], [1.337, 0.015, 0.26],
  ];

  for (let frame = 0; frame < frameCount; frame += 1) {
    left[frame] += ambienceLeft[frame] * variant.ambience;
    right[frame] += ambienceRight[frame] * variant.ambience;
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
    const drive = variant.drumColor === "industrial" ? 1.24 : 1.14;
    left[frame] = Math.tanh((left[frame] - meanLeft) * drive);
    right[frame] = Math.tanh((right[frame] - meanRight) * drive);
  }

  function repairBoundary(channel) {
    const repairFrames = Math.round(0.11 * sampleRate);
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
  const gain = variant.targetPeak / peak;
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

  const outputPath = path.resolve(`Assets/BGM/${variant.slug}_140BPM_32Bars_Loop.wav`);
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
  fs.writeFileSync(outputPath, wav);
  return { title: variant.title, output: outputPath, targetPeak: variant.targetPeak };
}

const requestedSlugs = process.argv.slice(2);
const selectedVariants = requestedSlugs.length === 0
  ? variants
  : variants.filter((variant) => requestedSlugs.includes(variant.slug));
const results = selectedVariants.map(renderVariant);
console.log(JSON.stringify({
  bpm,
  timeSignature: "4/4",
  bars,
  durationSeconds: totalSeconds,
  exactFrames: frameCount,
  smallestRhythmicGrid: "1/16 note",
  candidates: results,
}, null, 2));
