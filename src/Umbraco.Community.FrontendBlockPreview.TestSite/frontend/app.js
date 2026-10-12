// The whole "frontend": look the current path up in the Content Delivery API and render the page's blocks.
// /block-preview/{token}/ needs no special handling: the CMS answers that path with the previewed block.
const CMS = 'http://localhost:5170';

const escape = value =>
  String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);

// One function per block type, keyed by element type alias, like a component map in React or Vue
const blocks = {
  heroBlock: p => `
    <section class="hero hero--${escape((p.theme ?? 'Light').toLowerCase())}">
      <h1 class="hero__title">${escape(p.title)}</h1>
      <p class="hero__intro">${escape(p.intro)}</p>
      ${p.buttonText ? `<a class="button" href="#">${escape(p.buttonText)}</a>` : ''}
    </section>`,

  featuresBlock: p => `
    <section class="features">
      <h2 class="features__heading">${escape(p.heading)}</h2>
      <div class="features__grid">
        ${(p.cards?.items ?? [])
          .map(({ content }) => `
            <article class="card">
              <h3 class="card__title">${escape(content.properties.title)}</h3>
              <p class="card__text">${escape(content.properties.text)}</p>
            </article>`)
          .join('')}
      </div>
    </section>`,

  quoteBlock: p => `
    <figure class="quote">
      <blockquote class="quote__text">“${escape(p.quote)}”</blockquote>
      <figcaption class="quote__author">${escape(p.author)}</figcaption>
    </figure>`,
};

const response = await fetch(`${CMS}/umbraco/delivery/api/v2/content/item${location.pathname}`);
const app = document.getElementById('app');

if (!response.ok) {
  app.innerHTML = `<section class="hero hero--light"><h1 class="hero__title">Page not found</h1></section>`;
} else {
  const page = await response.json();

  app.innerHTML = (page.properties.blocks?.items ?? [])
    .map(({ content }) => blocks[content.contentType]?.(content.properties) ?? '')
    .join('');
}
