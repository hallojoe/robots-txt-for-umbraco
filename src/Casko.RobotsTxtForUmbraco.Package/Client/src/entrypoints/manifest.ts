export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "entryPoint",
    name: "Robots.txt Entry Point",
    alias: "Casko.RobotsTxtForUmbraco.EntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
