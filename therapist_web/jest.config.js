const nextJest = require('next/jest');

const createJestConfig = nextJest({ dir: './' });

/** @type {import('jest').Config} */
const customConfig = {
  testEnvironment: 'jest-environment-jsdom',
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  collectCoverageFrom: [
    'app/**/*.{ts,tsx}',
    'lib/**/*.{ts,tsx}',
    '!app/layout.tsx',
    '!app/globals.css',
    '!**/*.d.ts',
  ],
  coverageThreshold: {
    global: {
      lines: 85,
      branches: 85,
      functions: 85,
      statements: 85,
    },
  },
};

module.exports = createJestConfig(customConfig);
