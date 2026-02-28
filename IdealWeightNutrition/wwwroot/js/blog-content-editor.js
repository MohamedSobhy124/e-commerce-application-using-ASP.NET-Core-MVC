/**
 * Block-based blog content editor - Add Header or Paragraph blocks.
 * Converts blocks to HTML and stores in hidden input.
 */
(function () {
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function parseHtmlToBlocks(html) {
        const blocks = [];
        if (!html || !html.trim()) return blocks;
        const div = document.createElement('div');
        div.innerHTML = html;
        for (const el of div.children) {
            const tag = el.tagName.toLowerCase();
            if (tag === 'h2' || tag === 'h1' || tag === 'h3') {
                blocks.push({ type: 'header', text: el.innerHTML });
            } else if (tag === 'p') {
                blocks.push({ type: 'paragraph', text: el.innerHTML.replace(/<br\s*\/?>/gi, '\n') });
            } else if (el.innerHTML.trim()) {
                blocks.push({ type: 'paragraph', text: el.innerHTML.replace(/<br\s*\/?>/gi, '\n') });
            }
        }
        return blocks;
    }

    function blocksToHtml(blocks) {
        return blocks.map(b => {
            if (b.type === 'header') {
                return '<h2>' + escapeHtml(b.text).replace(/\n/g, ' ') + '</h2>';
            }
            return '<p>' + escapeHtml(b.text).replace(/\n/g, '<br>') + '</p>';
        }).join('');
    }

    function createBlockEl(block, index, dir) {
        const div = document.createElement('div');
        div.className = 'content-block content-block-' + block.type;
        div.dataset.index = index;
        div.dataset.type = block.type;

        const headerRow = document.createElement('div');
        headerRow.className = 'content-block-meta';
        const label = document.createElement('span');
        label.className = 'content-block-type-badge';
        label.innerHTML = block.type === 'header' ? '<i class="bi bi-type-h1"></i> Header' : '<i class="bi bi-paragraph"></i> Paragraph';

        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'content-block-remove';
        removeBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
        removeBtn.title = 'Remove block';

        headerRow.appendChild(label);
        headerRow.appendChild(removeBtn);

        let input;
        if (block.type === 'header') {
            input = document.createElement('input');
            input.type = 'text';
            input.className = 'form-control block-input block-input-header';
            input.placeholder = 'Enter heading...';
            input.value = block.text || '';
        } else {
            input = document.createElement('textarea');
            input.className = 'form-control block-input block-input-paragraph';
            input.rows = 4;
            input.placeholder = 'Write your paragraph...';
            input.value = block.text || '';
        }
        if (dir) input.setAttribute('dir', dir);

        div.appendChild(headerRow);
        div.appendChild(input);

        return div;
    }

    function initEditor(container) {
        const fieldName = container.closest('.content-block-editor').dataset.field;
        const blocksContainer = container;
        const hiddenInput = document.getElementById('content-hidden-' + fieldName);
        const dir = container.closest('.content-block-editor')?.dataset.dir || '';

        let blocks = parseHtmlToBlocks(hiddenInput?.value || '');

        function render() {
            blocksContainer.innerHTML = '';
            blocks.forEach((block, i) => {
                const el = createBlockEl(block, i, dir);
                blocksContainer.appendChild(el);

                const input = el.querySelector('.block-input');
                input.addEventListener('input', () => {
                    blocks[i].text = input.value;
                    syncToHidden();
                });

                el.querySelector('button').addEventListener('click', () => {
                    blocks.splice(i, 1);
                    render();
                });
            });
            if (blocks.length === 0) {
                blocksContainer.innerHTML = '<div class="content-empty-state"><div class="content-empty-icon"><i class="bi bi-blockquote-left"></i></div><p class="content-empty-title">Start building your content</p><p class="content-empty-hint">Add a header or paragraph block below</p></div>';
            }
            syncToHidden();
        }

        function syncToHidden() {
            if (hiddenInput) hiddenInput.value = blocksToHtml(blocks);
        }

        container.closest('.content-block-editor').querySelectorAll('.add-block-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const type = btn.dataset.type;
                blocks.push({ type, text: '' });
                render();
            });
        });

        render();
    }

    document.querySelectorAll('.content-block-editor').forEach(editor => {
        const container = editor.querySelector('.content-blocks-container');
        if (container) initEditor(container);
    });
})();
