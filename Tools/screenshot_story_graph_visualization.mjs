import playwright from "file:///C:/Users/BlueDevil/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.js";
import { pathToFileURL } from "node:url";

const { chromium } = playwright;

const input = "D:/UnityHub/Project/NGO/tools/character-story-graphs-standalone.html";
const browser = await chromium.launch({
  headless: true,
  executablePath: "C:/Program Files/Google/Chrome/Application/chrome.exe",
});
try {
  for (const [name, width] of [["desktop", 736], ["mobile", 360]]) {
    const page = await browser.newPage({ viewport: { width, height: 1200 }, deviceScaleFactor: 1 });
    await page.goto(pathToFileURL(input).href);
    await page.waitForTimeout(250);
    const frame = page.frames().find(candidate => candidate !== page.mainFrame());
    await frame.getByRole("button", { name: "周曜", exact: true }).click();
    await frame.getByRole("button", { name: /A05/ }).click();
    const interactionResult = await frame.locator("#storyGraphDetail").textContent();
    await frame.getByRole("button", { name: "刘丑", exact: true }).click();
    await page.screenshot({
      path: `D:/UnityHub/Project/NGO/generated_story_graphs/visualization-${name}.png`,
      fullPage: true,
    });
    const diagnostics = await frame.evaluate(() => ({
      scrollWidth: document.documentElement.scrollWidth,
      clientWidth: document.documentElement.clientWidth,
      buttons: document.querySelectorAll("button").length,
      flowText: document.getElementById("storyGraphFlow")?.textContent?.trim().length ?? 0,
      detailText: document.getElementById("storyGraphDetail")?.textContent?.trim().length ?? 0,
    }));
    diagnostics.interactionPassed = interactionResult?.includes("A05") ?? false;
    console.log(name, diagnostics);
    await page.close();
  }
} finally {
  await browser.close();
}
