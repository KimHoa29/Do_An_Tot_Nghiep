// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function showDocumentPopup(documentId) {
    // Remove any existing popup
    const existingPopup = document.querySelector('.document-popup-overlay');
    if (existingPopup) {
        existingPopup.remove();
    }

    // Create and show new popup
    fetch(`/Documents/PopupPartial/${documentId}`)
        .then(response => response.text())
        .then(html => {
            document.body.insertAdjacentHTML('beforeend', html);
            // Scroll to top of popup
            const popup = document.querySelector('.document-popup-overlay');
            if (popup) {
                popup.scrollTop = 0;
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi tải nội dung. Vui lòng thử lại sau.');
        });
}

function closeDocumentPopup(event) {
    if (event) event.preventDefault();
    const popup = document.querySelector('.document-popup-overlay');
    if (popup) popup.remove();
}

function toggleCommentBlock(documentId) {
    const block = document.getElementById(`comment-block-${documentId}`);
    if (!block) return;
    const wasHidden = block.style.display === 'none';
    block.style.display = wasHidden ? 'block' : 'none';
    if (wasHidden) {
        const textarea = block.querySelector('textarea');
        if (textarea) textarea.focus();
    }
}

function toggleReplyForm(commentId) {
    const allReplyForms = document.querySelectorAll('.reply-form');
    allReplyForms.forEach(form => {
        if (form.id !== `reply-form-${commentId}`) {
            form.style.display = 'none';
        }
    });
    const form = document.getElementById(`reply-form-${commentId}`);
    if (!form) return;
    const wasHidden = form.style.display === 'none';
    form.style.display = wasHidden ? 'block' : 'none';
    if (wasHidden) {
        const textarea = form.querySelector('textarea');
        if (textarea) {
            textarea.focus();
            textarea.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
}

function toggleDocumentMenu(documentId) {
    event.stopPropagation();
    const menu = document.getElementById(`document-menu-${documentId}`);
    if (!menu) return;
    const isVisible = menu.classList.contains('show');
    // Hide all menus first
    document.querySelectorAll('.post-menu, .document-menu, .topic-menu').forEach(m => {
        m.classList.remove('show');
    });
    // Show this menu if it wasn't visible
    if (!isVisible) {
        menu.classList.add('show');
    }
}

function deleteDocument(documentId) {
    if (confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) {
        window.location.href = '/Documents/Delete/' + documentId;
    }
}

function toggleDocumentLike(documentId) {
    const likeBtn = document.querySelector(`.like-btn[data-document-id="${documentId}"]`);
    if (!likeBtn) return;
    const likeCountSpan = likeBtn.querySelector('.count-number');
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';
    fetch(`/Documents/ToggleLike/${documentId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (data.isLiked) {
                likeBtn.classList.add('liked');
            } else {
                likeBtn.classList.remove('liked');
            }
            likeCountSpan.textContent = data.likeCount;
        } else {
            alert(data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Có lỗi xảy ra khi thực hiện thao tác. Vui lòng thử lại sau.');
    });
}

// Close menus when clicking outside
if (typeof window !== 'undefined') {
    document.addEventListener('click', function(event) {
        if (!event.target.closest('.post-actions') &&
            !event.target.closest('.document-actions') &&
            !event.target.closest('.topic-actions')) {
            document.querySelectorAll('.post-menu, .document-menu, .topic-menu').forEach(menu => {
                menu.classList.remove('show');
            });
        }
    });

    // Prevent menu from closing when clicking inside
    document.querySelectorAll('.post-menu, .document-menu, .topic-menu').forEach(menu => {
        menu.addEventListener('click', function(event) {
            event.stopPropagation();
        });
    });
}
