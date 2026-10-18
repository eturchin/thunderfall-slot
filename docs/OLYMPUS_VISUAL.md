# Winter Olympus visual direction

Implemented from the user's [slot screenshot reference](https://great.com/wp-content/uploads/2024/12/13122024_1734107659.jpg): heavy condensed labels, gold outlined display lettering, a purple reel field, gold bevels, bright winter scenery and compact controls. Thunderfall retains its own name, symbols, game rules and 5,000x cycle maximum.

## Assets and typography

- Background: `frontend/public/assets/olympus-winter-v1.png`, generated with the built-in imagegen tool, copied from its generated-images output into this workspace.
- Original SVG symbol revision: `frontend/public/assets/symbols/`, eleven symbols with gold edges, saturated facets and distinct silhouettes. These were edited as SVG source, not generated raster images.
- Fonts: `frontend/public/assets/fonts/Anton-Regular.ttf` for game labels/numbers and `CinzelDecorative-Black.ttf` for the logo. Each has an adjacent upstream SIL OFL license file. No external font request is needed at runtime.
- Theme: `frontend/src/app/App.module.css` and `global.css`; canvas palette and cell spacing: `frontend/src/renderer/StormScene.ts`.
- Screenshots: `reports/olympus-desktop.png`, `reports/olympus-mobile.png`.

## Background generation prompt

Built-in imagegen, one newly generated opaque landscape illustration; no reference artwork embedded in the application.

Create a production background illustration for the original browser slot game THUNDERFALL, no text anywhere. Wide landscape 1536x1024. Luxurious bright winter Mount Olympus fantasy game art, polished richly rendered 3D illustration, brilliant azure sky, luminous white clouds, ivory marble Greek columns with ornate sculpted gold capitals on the far left and far right, snow on ledges, pale sunlit marble terrace floor at bottom, icy blue distant mountains. Composition: central 65 percent of image mostly calm lavender sky and hazy snowy temple backdrop, because a large purple slot board will cover this center. At the far RIGHT edge (rightmost 20 percent), a heroic original elderly storm titan with a long silver beard and long white hair, ornate golden armor and deep burgundy flowing cloak, standing three-quarter full-body, holding an electric cyan lightning bolt, looking toward center. His head must be in upper right quadrant below top 12 percent; full body occupying right 70 percent of image height. Bright colorful casino fantasy aesthetic, crisp gold details, warm light on skin, subtle frost and sparkling snow, confident benevolent face. Space for left-side controls occupies left 15 percent; no foreground figures there. The composition should feel celebratory and elegant, with strong purple/gold/ivory/sky-blue palette. No UI, no lettering, no logos, no numbers, no frames around a slot grid, no reel symbols. Artwork only.
