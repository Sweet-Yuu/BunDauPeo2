# Nguồn và prompt artwork Restaurant

Tạo bằng **ImageGen tích hợp**, không dùng CLI/API key, ngày 09–10/11/2026. User duyệt hướng hình ảnh 1 và yêu cầu remake lẫn Lobby gốc. Các PNG được chép nguyên bản vào repo; sprite cắt bằng TextureImporter. `frame.png` là primitive bo góc 9-slice sinh bằng Editor code, không phải artwork AI.

## File và thứ tự sprite

- `Assets/_Project/UI/Resources/RestaurantArt/skills-facilities.png`: sheet 4×4, trái sang phải, trên xuống dưới: cooking, revenue, patience, ingredients; cleanliness, hospitality, master_chef, master_owner; master_host, generator, stove, freezer; purifier, wire, relay, register.
- `.../rewards.png`: sheet 4×2: coin, diamond, ingredient_basket, gift; pouch, chest, vault, platter.
- `.../garden.png`: nền sân quán, 16:9, không chữ/UI.
- `Mota/restaurant-skills-art-preview.png`: ảnh mẫu user chọn, không dùng làm ảnh phẳng thay cho UI thật.

## Prompt chung cho hai sheet

Game production asset for BUN DAU PEO Vietnamese restaurant management game. Cozy polished hand-painted 2D casual mobile game art, warm cream and honey-gold highlights, jade green, cocoa outlines, subtle soft shadows attached to each object, vivid readable shapes at 64px, original art. NO lettering, numbers, labels, tiles, interface, frames, watermarks, checkerboard, or background. Real transparent background.

## Prompt nối tiếp — skills-facilities

Create a SQUARE 4-column by 4-row sprite atlas of EXACTLY 16 separate icons, each centered in its own equally-sized square cell, all cells same size, arranged in perfectly regular grid. Leave at least 16% empty transparent margin within EACH cell, never overlap neighboring cells. Individual objects not touching. Consistent slightly front-facing three-quarter view, no floor. Read cells LEFT TO RIGHT, TOP TO BOTTOM: row 1: chef hat over small frying wok; recipe book with unlettered cover; Vietnamese blue porcelain teacup with steam; basket of fresh herbs and vegetables. Row 2: folded clean kitchen towel with sparkle; wooden serving tray with small tofu dish; prestigious chef hat with crossed utensils badge; miniature Vietnamese tiled-roof restaurant badge. Row 3: teacup with small warm heart and gold laurel badge; compact restaurant electrical generator green metal box with bolt symbol; commercial cooking stove with frying pot; industrial refrigerator cabinet pale jade. Row 4: air purifier white with green vent; red-copper coiled power cable with plug; small electrical relay box jade with yellow bolt; compact wood-and-jade cash register. This exact row-major order is critical. Icons independent assets, not a mockup. Square sheet.

## Prompt nối tiếp — rewards

Create a WIDE 4-column by 2-row sprite atlas of EXACTLY 8 separate icons, each centered in its own equally-sized square cell. Regular grid with equal rows and columns and at least 16% transparent padding inside EACH cell, absolutely no overlap. Row 1 left to right: a short stack of shiny gold coins with simple circular embossed motif; a large faceted turquoise-blue diamond; woven basket of herbs vegetables and tofu ingredients; generous wrapped daily gift box cream paper jade ribbon with small gold star. Row 2 left to right: small full gold-coin pouch; wooden chest filled with gold coins; larger ornate jade-and-gold treasure vault filled with gold; a round bamboo platter of Vietnamese bun dau with fried tofu cubes, white rice noodles, green herbs, cucumber slices and dipping sauce. Exact order critical, isolated readable objects, no words or characters.

## Prompt — garden

Use case: stylized-concept. A polished original hand-painted 2D landscape BACKGROUND asset for a Vietnamese bun dau restaurant management game's UI, wide 16:9. View from a slightly elevated camera into a cozy courtyard restaurant, decorative framing only: soft warm late-afternoon light, leafy banana plants at far corners, bamboo fence near far top edge, terracotta roof edge, hanging lantern at far left and terracotta pots near lower corners. Central 75 percent is very quiet, softly mottled pale warm cream/light sage ground, spacious and low contrast with absolutely NO objects in its center, no tables no chairs no people. Most detailed foliage and restaurant architectural details restricted to outermost 10-15 percent margins. Jade green, honey wood, cream and restrained coral. Strong professional mobile-game illustration quality, gentle paper brush texture, clean silhouettes, charming cozy mood. This will sit behind a functional restaurant floor and UI panels, so negative space is essential. No text, no logos, no buttons, no UI panels, no watermarks. Opaque full background.
