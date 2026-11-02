
using System.Collections;
using UnityEngine;

// Lægges på fjendens Gun (som child af Enemy).
// Gun drejer rundt om fjenden mod spilleren og skyder når den sigter rigtigt.
public class EnemyGun : MonoBehaviour
{
	[Header("Placering")]
	public Enemy enemy;
	public float distanceFromEnemy = 0.7f;

	[Header("Sigte")]
	public float turnSpeed = 270f;
	public float aimTolerance = 10f;
	public float reactionTime = 0.4f;

	[Header("Bullet")]
	public GameObject bulletPrefab;
	public Transform spawnPoint;
	public float bulletSpeed = 10f;
	public int damage = 1;

	[Min(1)]
	public int bulletsPerShot = 1;

	[Range(0f, 180f)]
	public float spread = 30f;

	[Header("Lyd (må gerne være tom)")]
	public AudioClip shootSound;
	public float minPitch = 0.9f;
	public float maxPitch = 1.1f;

	[Header("Skud")]
	public float shootInterval = 0.4f;
	public int magazineSize = 6;
	public float reloadTime = 1.5f;

	private float currentAngle = 0f;
	private float shootTimer = 0f;
	private float reactionTimer = 0f;
	private int currentAmmo;
	private bool isReloading = false;
	private Vector3 startScale;

	void Start()
	{
		if (enemy == null)
		{
			enemy = GetComponentInParent<Enemy>();
		}

		startScale = transform.localScale;
		currentAmmo = magazineSize;
	}

	void Update()
	{
		if (enemy == null) return;

		if (shootTimer > 0f)
		{
			shootTimer -= Time.deltaTime;
		}

		if (enemy.canSeePlayer)
		{
			// Drej langsomt mod spilleren
			Vector2 toPlayer =
				enemy.player.position - enemy.transform.position;

			float targetAngle =
				Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

			currentAngle = Mathf.MoveTowardsAngle(
				currentAngle,
				targetAngle,
				turnSpeed * Time.deltaTime
			);

			reactionTimer += Time.deltaTime;

			// Skyd hvis vi sigter nogenlunde rigtigt
			bool isAimed =
				Mathf.Abs(Mathf.DeltaAngle(
					currentAngle,
					targetAngle
				)) <= aimTolerance;

			if (isAimed && reactionTimer >= reactionTime)
			{
				TryShoot();
			}
		}
		else
		{
			// Mister den spilleren, skal den reagere igen
			reactionTimer = 0f;
		}

		UpdateGunTransform();
	}

	void UpdateGunTransform()
	{
		Vector2 direction = new Vector2(
			Mathf.Cos(currentAngle * Mathf.Deg2Rad),
			Mathf.Sin(currentAngle * Mathf.Deg2Rad)
		);

		// Placér gun rundt om fjenden
		transform.position =
			enemy.transform.position +
			(Vector3)(direction * distanceFromEnemy);

		transform.rotation =
			Quaternion.Euler(0f, 0f, currentAngle);

		// Vend gun på venstre side
		Vector3 scale = startScale;

		scale.y = direction.x < 0
			? -Mathf.Abs(startScale.y)
			: Mathf.Abs(startScale.y);

		transform.localScale = scale;
	}

	void TryShoot()
	{
		if (isReloading || shootTimer > 0f) return;

		if (currentAmmo <= 0)
		{
			StartCoroutine(Reload());
			return;
		}

		Shoot();
	}

	void Shoot()
	{
		if (bulletPrefab == null) return;

		Transform firePoint =
			spawnPoint != null ? spawnPoint : transform;

		int bulletCount = Mathf.Max(1, bulletsPerShot);

		// Skyd alle bullets samtidig
		for (int i = 0; i < bulletCount; i++)
		{
			float angleOffset = 0f;

			// Fordel bullets ligeligt over spread
			if (bulletCount > 1)
			{
				float step = spread / (bulletCount - 1);

				angleOffset =
					-spread / 2f + step * i;
			}

			Quaternion rotation =
				firePoint.rotation *
				Quaternion.Euler(0f, 0f, angleOffset);

			// Opret bullet
			GameObject bullet = Instantiate(
				bulletPrefab,
				firePoint.position,
				rotation
			);

			// Sæt damage og ejerskab
			Bullet bulletScript =
				bullet.GetComponent<Bullet>();

			if (bulletScript != null)
			{
				bulletScript.ownerTag = enemy.tag;
				bulletScript.owner = enemy.transform;
				bulletScript.damage = damage;
			}

			// Giv bullet hastighed
			Rigidbody2D bulletRb =
				bullet.GetComponent<Rigidbody2D>();

			if (bulletRb != null)
			{
				bulletRb.gravityScale = 0f;

				bulletRb.linearVelocity =
					rotation * Vector3.right * bulletSpeed;
			}
		}

		// Afspil kun én lyd pr. affyring
		SoundPlayer.Play(
			shootSound,
			firePoint.position,
			minPitch,
			maxPitch
		);

		// Bruger kun ét skud fra magasinet
		currentAmmo--;

		shootTimer = shootInterval;
	}

	IEnumerator Reload()
	{
		isReloading = true;

		yield return new WaitForSeconds(reloadTime);

		currentAmmo = magazineSize;
		isReloading = false;
	}
}
