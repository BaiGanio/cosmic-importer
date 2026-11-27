# Cosmic: image rules for the agent (draft, X01 D57)

Lyuben edits this file. The agent reads it for every image in "Kept" (image + page text)
and answers which rules the image breaks. One broken rule: the image goes to "Dropped",
marked "agent: R<n>", and you can bring it back. A new kind of leftover: add a rule, run again.
Keep each rule short, one idea per line. Do not reuse a number.

## Keep an image only when

- It is a real photograph (telescope or camera) of the sky.
- The object of this page is the main subject.
- It breaks none of the rules below.

## Drop rules

- R1: A cover or a page of a magazine, newsletter, calendar, book, poster or brochure.
- R2: A logo, a title or a caption is part of the picture. (A small credit line in a corner is fine.)
- R3: Several panels, an inset, a zoom box, or a frame around the picture.
- R4: Labels, arrows, circles, a scale bar, a compass, a grid or coordinates drawn on the picture.
- R5: A diagram, chart, graph, spectrum, map or sky chart.
- R6: An artist's impression, drawing, painting, 3D render or simulation.
- R7: A screenshot of software or of a website.
- R8: A photo of a telescope, an observatory, an instrument, people or a landscape, where the sky object is small or not the main subject.
- R9: The picture shows another object, not the object of this page.
- R10: Too dark, too blurry, or mostly empty.

## Answer (JSON only)

{"keep": true or false, "broken": ["R1", ...], "score": 0 to 10, "reason": "at most 15 words"}
