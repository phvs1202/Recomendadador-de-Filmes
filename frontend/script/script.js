/* script.js */
const API_URL = "https://localhost:7061/api/Recommendations"; // Verifique a porta da sua API

// 1. Controle de Sessão
function checkAuth() {
    const userId = localStorage.getItem('currentUserId');
    if (!userId) {
        window.location.href = 'index.html';
    }
    return userId;
}

function logout() {
    localStorage.removeItem('currentUserId');
    window.location.href = './index.html';
}

// 2. Carregar Todos os Filmes (Para a tela de Avaliação)
async function loadAllMovies() {
    const userId = checkAuth();
    const grid = document.getElementById('moviesGrid');

    try {
        // MUDANÇA 1: Agora chamamos passando o ID do usuário para pegar as notas dele
        const response = await fetch(`${API_URL}/movies/${userId}`);

        if (!response.ok) throw new Error("Erro na API");

        const movies = await response.json();

        grid.innerHTML = '';

        movies.forEach(movie => {
            const card = document.createElement('div');
            card.className = 'movie-card';

            // MUDANÇA 2: Lógica para decidir entre IMAGEM ou PLACEHOLDER
            let imageHtml;
            if (movie.photo && movie.photo.startsWith('http')) {
                // Se tem link de foto, usa a imagem
                imageHtml = `<img src="${movie.photo}" alt="${movie.title}" class="movie-poster-img" onerror="this.style.display='none';this.nextElementSibling.style.display='flex'">`;
                // O código 'onerror' acima serve de backup: se a imagem quebrar, ele esconde a imagem e mostra o placeholder abaixo
            }

            // Placeholder de backup (caso não tenha foto ou falhe)
            const placeholderHtml = `<div class="poster-placeholder" ${movie.photo ? 'style="display:none"' : ''}>${movie.title.substring(0, 2)}</div>`;

            card.innerHTML = `
                ${imageHtml || ''}
                ${placeholderHtml}
                
                <div class="movie-info">
                    <div class="movie-title">${movie.title}</div>
                    <div class="movie-meta">
                        <span>${movie.year}</span>
                        <span>${movie.genre}</span>
                    </div>
                    <div class="rating-stars" data-movie-id="${movie.id}" data-user-rating="${movie.myRating}">
                        ${generateStarsHTML(movie.myRating)}
                    </div>
                </div>
            `;
            grid.appendChild(card);
        });

        setupStarEvents(userId);

    } catch (error) {
        console.error("Erro ao carregar filmes:", error);
        grid.innerHTML = '<p>Erro ao conectar com a API.</p>';
    }
}

// 3. Carregar Recomendações
async function loadRecommendations() {
    const userId = checkAuth();
    const grid = document.getElementById('recommendationsGrid');

    try {
        const response = await fetch(`${API_URL}/recommend/${userId}`);
        const recommendations = await response.json();

        grid.innerHTML = '';

        if (recommendations.length === 0) {
            grid.innerHTML = '<p>Avalie alguns filmes para receber recomendações!</p>';
            return;
        }

        recommendations.forEach(rec => {
            const card = document.createElement('div');
            card.className = 'movie-card';
            // Nota prevista formatada
            const score = rec.predictedRating.toFixed(1);

            card.innerHTML = `
                <div class="recommendation-score">${score} ★</div>
                <div class="poster-placeholder" style="background: linear-gradient(45deg, #590000, #b30000); color: white;">
                    ${rec.title.substring(0, 2)}
                </div>
                <div class="movie-info">
                    <div class="movie-title">${rec.title}</div>
                    <div class="movie-meta">
                    </div>
                </div>
            `;
            grid.appendChild(card);
        });

    } catch (error) {
        console.error("Erro:", error);
        grid.innerHTML = '<p>Erro ao buscar recomendações.</p>';
    }
}

/* --- NOVAS FUNÇÕES DO MODAL --- */
function showModal(title, message) {
    const modal = document.getElementById('customModal');
    const titleEl = modal.querySelector('.modal-title');
    const msgEl = document.getElementById('modalMessageText');

    if (title) titleEl.innerText = title;
    msgEl.innerText = message;

    modal.classList.add('show');
}

function closeModal() {
    const modal = document.getElementById('customModal');
    modal.classList.remove('show');
}

// Fecha o modal se clicar fora da caixa (no fundo escuro)
window.onclick = function (event) {
    const modal = document.getElementById('customModal');
    if (event.target == modal) {
        closeModal();
    }
}

// 4. Lógica das Estrelas (UI) e Envio de Nota
// MUDANÇA: Recebe o currentRating
function generateStarsHTML(currentRating) {
    let stars = '';
    for (let i = 1; i <= 5; i++) {
        // Se a nota atual for maior ou igual a 'i', adiciona a classe 'active'
        const activeClass = i <= currentRating ? 'active' : '';
        stars += `<span class="star ${activeClass}" data-value="${i}">★</span>`;
    }
    return stars;
}

function setupStarEvents(userId) {
    const starContainers = document.querySelectorAll('.rating-stars');

    starContainers.forEach(container => {
        const stars = container.querySelectorAll('.star');
        const movieId = container.dataset.movieId;

        // Pega a nota salva inicialmente
        let currentSavedRating = parseFloat(container.dataset.userRating || 0);

        stars.forEach(star => {
            // HOVER: Mostra visualmente a nota que o mouse está em cima
            star.addEventListener('mouseover', () => {
                const hoverValue = star.dataset.value;
                highlightStars(stars, hoverValue);
            });

            // CLIQUE: Salva ou Edita a nota
            star.addEventListener('click', async () => {
                const newValue = star.dataset.value;

                // Feedback visual imediato (Opcional, mas melhora a sensação de rapidez)
                highlightStars(stars, newValue);

                // Envia para API
                const success = await submitRating(userId, movieId, newValue);

                if (success) {
                    // Atualiza a "Nota Salva" no container
                    container.dataset.userRating = newValue;
                    currentSavedRating = newValue; // Atualiza a variável local também

                    // Exibe o Modal Bonito
                    showModal("Sucesso!", `Você avaliou este filme com nota ${newValue}.`);
                } else {
                    // Se falhar, volta as estrelas para a nota anterior
                    highlightStars(stars, currentSavedRating);
                    showModal("Erro", "Não foi possível salvar sua avaliação.");
                }
            });
        });

        // MOUSE LEAVE: Quando tirar o mouse, volta para a nota REALMENTE salva (seja a antiga ou a nova editada)
        container.addEventListener('mouseleave', () => {
            // Importante: lê sempre do dataset atualizado
            const saved = parseFloat(container.dataset.userRating || 0);
            highlightStars(stars, saved);
        });
    });
}

/* --- FUNÇÃO DE ENVIO ATUALIZADA --- */
async function submitRating(userId, movieId, rating) {
    const payload = {
        userId: Number(userId),  // Força conversão para número
        movieId: Number(movieId), // Força conversão para número
        rating: parseFloat(rating)
    };

    try {
        const response = await fetch(`${API_URL}/rate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        return response.ok; // Retorna true se deu certo (200 OK)
    } catch (error) {
        console.error("Erro ao avaliar:", error);
        return false;
    }
}

function highlightStars(stars, value) {
    stars.forEach(star => {
        if (star.dataset.value <= value) {
            star.classList.add('active');
        } else {
            star.classList.remove('active');
        }
    });
}

function resetStars(stars) {
    stars.forEach(star => star.classList.remove('active'));
}

// 6. Login do Usuário
async function loginUser(username, password) {
    const loginEndpoint = `${API_URL}/login`; // Seu novo endpoint

    const payload = {
        name: username,
        password: password
    };

    try {
        const response = await fetch(loginEndpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const data = await response.json();

            // É CRUCIAL que o seu endpoint /login retorne o userId
            if (data && data.userId) {
                localStorage.setItem('currentUserId', data.userId);
                return true; // Login bem-sucedido
            }
            return false; // Login falhou (resposta 200, mas sem ID)
        } else if (response.status === 401) {
            // Não autorizado
            return false;
        } else {
            console.error("Erro no servidor:", await response.text());
            return false;
        }
    } catch (error) {
        throw error; // Deixa o erro ser tratado pelo bloco catch do handleLogin
    }
}