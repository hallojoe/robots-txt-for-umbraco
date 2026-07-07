import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { manifests as workspace } from "./workspace/manifest.js";

export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...workspace,
];
